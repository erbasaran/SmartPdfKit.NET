using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmartPdfKit.Exceptions;
using SmartPdfKit.Interfaces;
using SmartPdfKit.Models;
using Tesseract;

namespace SmartPdfKit.Infrastructure.Ocr
{
	/// <summary>
	/// Implements <see cref="IOcrProvider"/> using CharlesW Tesseract OCR engine.
	/// </summary>
	public class TesseractOcrProvider : IOcrProvider
	{
		private readonly IPdfRenderer _pdfRenderer;

		/// <summary>
		/// Initializes a new instance of the <see cref="TesseractOcrProvider"/> class.
		/// </summary>
		/// <param name="pdfRenderer">The PDF page renderer.</param>
		public TesseractOcrProvider(IPdfRenderer pdfRenderer)
		{
			_pdfRenderer = pdfRenderer ?? throw new ArgumentNullException(nameof(pdfRenderer));
		}

		/// <inheritdoc/>
		public async Task<OcrResult> PerformOcrAsync(Stream pdfStream, OcrOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));

			// Render PDF pages to temporary PNG streams first
			var imageConversionOptions = new ImageConversionOptions
			{
				Format = SmartPdfKit.Models.ImageFormat.Png,
				Dpi = options.Dpi,
				Password = options.Password
			};

			var imageStreams = await _pdfRenderer.RenderToImagesAsync(pdfStream, imageConversionOptions, cancellationToken).ConfigureAwait(false);

			try
			{
				return await Task.Run(() =>
				{
					string? tessdataPath = options.TessDataPath;

					// Try standard locations if not specified
					if (string.IsNullOrEmpty(tessdataPath))
					{
						tessdataPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
						if (string.IsNullOrEmpty(tessdataPath))
						{
							tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
						}
					}

					if (!Directory.Exists(tessdataPath))
					{
						throw new PdfOcrException($"Tessdata directory not found at: '{tessdataPath}'. Please download the relevant language files (.traineddata) from https://github.com/tesseract-ocr/tessdata_best and place them in that folder.");
					}

					TesseractEngine engine;
					try
					{
						engine = new TesseractEngine(tessdataPath, options.Language, EngineMode.Default);
					}
					catch (Exception ex)
					{
						throw new PdfOcrException($"Failed to initialize Tesseract OCR engine with language '{options.Language}' using data path '{tessdataPath}'. Please verify Tesseract is installed and language data files exist.", ex);
					}

					using (engine)
					{
						var textBuilder = new StringBuilder();
						float totalConfidence = 0;
						int pageCount = 0;

						if (options.Mode == OcrMode.TextOnly)
						{
							foreach (var imgStream in imageStreams)
							{
								cancellationToken.ThrowIfCancellationRequested();
								using (imgStream)
								{
									byte[] bytes = Helpers.StreamHelpers.StreamToBytes(imgStream);
									using (var pix = Pix.LoadFromMemory(bytes))
									{
										using (var page = engine.Process(pix))
										{
											textBuilder.AppendLine(page.GetText());
											totalConfidence += page.GetMeanConfidence();
											pageCount++;
										}
									}
								}
							}

							float confidence = pageCount > 0 ? totalConfidence / pageCount : 0;
							return new OcrResult(textBuilder.ToString(), confidence);
						}
						else // OcrMode.SearchablePdf
						{
							string tempFolder = Path.Combine(Path.GetTempPath(), "SmartPdfKit_temp_ocr_" + Guid.NewGuid().ToString("N"));
							Directory.CreateDirectory(tempFolder);
							string tempFileName = "ocr_result";
							string tempPathWithoutExt = Path.Combine(tempFolder, tempFileName);
							string actualPdfPath = tempPathWithoutExt + ".pdf";

							try
							{
								using (var renderer = ResultRenderer.CreatePdfRenderer(tempPathWithoutExt, tessdataPath, false))
								{
									using (renderer.BeginDocument("Searchable PDF"))
									{
										foreach (var imgStream in imageStreams)
										{
											cancellationToken.ThrowIfCancellationRequested();
											using (imgStream)
											{
												byte[] bytes = Helpers.StreamHelpers.StreamToBytes(imgStream);
												using (var pix = Pix.LoadFromMemory(bytes))
												{
													using (var page = engine.Process(pix))
													{
														textBuilder.AppendLine(page.GetText());
														totalConfidence += page.GetMeanConfidence();
														pageCount++;
														renderer.AddPage(page);
													}
												}
											}
										}
									}
								}

								cancellationToken.ThrowIfCancellationRequested();
								byte[] pdfBytes = File.ReadAllBytes(actualPdfPath);
								var searchableStream = new MemoryStream(pdfBytes);

								float confidence = pageCount > 0 ? totalConfidence / pageCount : 0;
								return new OcrResult(textBuilder.ToString(), confidence, searchableStream);
							}
							catch (Exception ex) when (!(ex is OperationCanceledException))
							{
								throw new PdfOcrException("Failed to generate searchable PDF using Tesseract.", ex);
							}
							finally
							{
								// Clean up files
								try
								{
									if (File.Exists(actualPdfPath)) File.Delete(actualPdfPath);
									if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder);
								}
								catch
								{
									// Ignore cleanup exceptions
								}
							}
						}
					}
				}, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				foreach (var imgStream in imageStreams)
				{
					imgStream?.Dispose();
				}
			}
		}
	}
}
