using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using SkiaSharp;
using SmartPdfKit.Exceptions;
using SmartPdfKit.Interfaces;
using SmartPdfKit.Models;
using PdfMetadata = SmartPdfKit.Models.PdfMetadata;

namespace SmartPdfKit.Infrastructure.Pdfsharp
{
	/// <summary>
	/// Implements <see cref="IPdfProcessor"/> using PDFsharp for core PDF editing operations.
	/// </summary>
	public class PdfsharpProcessor : IPdfProcessor
	{
		static PdfsharpProcessor()
		{
			RegisterFontResolver();
		}

		/// <inheritdoc/>
		public async Task<Stream> MergeAsync(IEnumerable<Stream> pdfStreams, CancellationToken cancellationToken = default)
		{
			if (pdfStreams == null) throw new ArgumentNullException(nameof(pdfStreams));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					using (var outputDocument = new PdfDocument())
					{
						foreach (var sourceStream in pdfStreams)
						{
							cancellationToken.ThrowIfCancellationRequested();
							var seekable = EnsureSeekable(sourceStream);
							try
							{
								using (var inputDocument = PdfReader.Open(seekable, PdfDocumentOpenMode.Import))
								{
									int count = inputDocument.PageCount;
									for (int idx = 0; idx < count; idx++)
									{
										cancellationToken.ThrowIfCancellationRequested();
										var page = inputDocument.Pages[idx];
										outputDocument.AddPage(page);
									}
								}
							}
							finally
							{
								if (seekable != sourceStream)
								{
									seekable.Dispose();
								}
							}
						}
						outputDocument.Save(outputStream);
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to merge PDF documents.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<IEnumerable<Stream>> SplitAsync(Stream pdfStream, SplitOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));

			return await Task.Run(() =>
			{
				var result = new List<Stream>();
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using var inputDocument = string.IsNullOrEmpty(options.Password)
							? PdfReader.Open(seekable, PdfDocumentOpenMode.Import)
							: PdfReader.Open(seekable, options.Password, PdfDocumentOpenMode.Import);

						var totalPages = inputDocument.PageCount;

						if (options.Mode == SplitMode.SplitAll)
						{
							for (int i = 0; i < totalPages; i++)
							{
								cancellationToken.ThrowIfCancellationRequested();
								var outDoc = new PdfDocument();
								outDoc.AddPage(inputDocument.Pages[i]);
								var ms = new MemoryStream();
								outDoc.Save(ms);
								ms.Position = 0;
								result.Add(ms);
							}
						}
						else if (options.Mode == SplitMode.SplitFixedSize)
						{
							int interval = options.PageInterval;
							for (int i = 0; i < totalPages; i += interval)
							{
								cancellationToken.ThrowIfCancellationRequested();
								var outDoc = new PdfDocument();
								int end = Math.Min(i + interval, totalPages);
								for (int j = i; j < end; j++)
								{
									outDoc.AddPage(inputDocument.Pages[j]);
								}
								var ms = new MemoryStream();
								outDoc.Save(ms);
								ms.Position = 0;
								result.Add(ms);
							}
						}
						else if (options.Mode == SplitMode.SplitByRanges)
						{
							if (string.IsNullOrWhiteSpace(options.Ranges))
							{
								throw new PdfProcessingException("Ranges must be specified for range-based splitting.");
							}

							var ranges = ParseRanges(options.Ranges ?? string.Empty, totalPages);
							foreach (var range in ranges)
							{
								cancellationToken.ThrowIfCancellationRequested();
								var outDoc = new PdfDocument();
								foreach (var pageNum in range)
								{
									outDoc.AddPage(inputDocument.Pages[pageNum - 1]);
								}
								var ms = new MemoryStream();
								outDoc.Save(ms);
								ms.Position = 0;
								result.Add(ms);
							}
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}

					return (IEnumerable<Stream>)result;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is PdfProcessingException))
				{
					foreach (var s in result) s.Dispose();
					throw new PdfProcessingException("Failed to split PDF document.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> CompressAsync(Stream pdfStream, CompressOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using (var document = string.IsNullOrEmpty(options.Password)
							? PdfReader.Open(seekable, PdfDocumentOpenMode.Modify)
							: PdfReader.Open(seekable, options.Password, PdfDocumentOpenMode.Modify))
						{
							document.Options.CompressContentStreams = options.CompressContentStreams;
							document.Options.FlateEncodeMode = PdfFlateEncodeMode.BestCompression;
							document.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Never;

							// Optimize PDF structural dictionaries losslessly (PieceInfo, Metadata, etc.)
							OptimizePdfStructure(document, options.RemoveMetadata);

							if (options.ImageQuality.HasValue && options.ImageQuality.Value > 0)
							{
								OptimizeImagesInDocument(document, options.ImageQuality.Value, options.MaxImageDimension);
							}

							// Lossless deduplication of duplicate images across the document
							DeduplicateImages(document);

							document.Save(outputStream);
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to compress PDF document.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> RotateAsync(Stream pdfStream, int rotationDegrees, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using (var document = PdfReader.Open(seekable, PdfDocumentOpenMode.Modify))
						{
							int count = document.PageCount;
							for (int i = 0; i < count; i++)
							{
								cancellationToken.ThrowIfCancellationRequested();
								var page = document.Pages[i];
								page.Rotate = (page.Rotate + rotationDegrees) % 360;
							}
							document.Save(outputStream);
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to rotate PDF document.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> CropAsync(Stream pdfStream, CropOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using (var document = string.IsNullOrEmpty(options.Password)
							? PdfReader.Open(seekable, PdfDocumentOpenMode.Modify)
							: PdfReader.Open(seekable, options.Password, PdfDocumentOpenMode.Modify))
						{
							int count = document.PageCount;
							for (int i = 0; i < count; i++)
							{
								cancellationToken.ThrowIfCancellationRequested();
								var page = document.Pages[i];
								double width = page.Width.Point;
								double height = page.Height.Point;

								double cropLeft, cropTop, cropRight, cropBottom;
								if (options.UsePercentage)
								{
									cropLeft = options.Left * width / 100.0;
									cropRight = options.Right * width / 100.0;
									cropTop = options.Top * height / 100.0;
									cropBottom = options.Bottom * height / 100.0;
								}
								else
								{
									cropLeft = options.Left;
									cropRight = options.Right;
									cropTop = options.Top;
									cropBottom = options.Bottom;
								}

								double newLeft = cropLeft;
								double newBottom = cropBottom;
								double newRight = width - cropRight;
								double newTop = height - cropTop;

								if (newRight <= newLeft || newTop <= newBottom)
								{
									throw new PdfProcessingException($"Invalid crop bounds on page {i + 1}. Crop margins are too large.");
								}

								page.CropBox = new PdfRectangle(new XPoint(newLeft, newBottom), new XPoint(newRight, newTop));
							}
							document.Save(outputStream);
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is PdfProcessingException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to crop PDF document.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> RemovePagesAsync(Stream pdfStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (pageNumbers == null) throw new ArgumentNullException(nameof(pageNumbers));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using (var document = PdfReader.Open(seekable, PdfDocumentOpenMode.Modify))
						{
							var sortedPages = pageNumbers.OrderByDescending(p => p).ToList();
							foreach (var pageNum in sortedPages)
							{
								cancellationToken.ThrowIfCancellationRequested();
								if (pageNum > 0 && pageNum <= document.PageCount)
								{
									document.Pages.RemoveAt(pageNum - 1);
								}
								else
								{
									throw new PdfProcessingException($"Page number {pageNum} is out of bounds. The PDF has only {document.PageCount} pages.");
								}
							}

							if (document.PageCount == 0)
							{
								throw new PdfProcessingException("Cannot remove all pages from a PDF. At least 1 page must remain.");
							}

							document.Save(outputStream);
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is PdfProcessingException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to remove pages from PDF document.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> TextToPdfAsync(string text, TextToPdfOptions options, CancellationToken cancellationToken = default)
		{
			if (text == null) throw new ArgumentNullException(nameof(text));
			if (options == null) throw new ArgumentNullException(nameof(options));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					RegisterFontResolver();
					using (var document = new PdfDocument())
					{
						var page = document.AddPage();
						var gfx = XGraphics.FromPdfPage(page);

						var font = new XFont(options.FontName, options.FontSize, XFontStyleEx.Regular);
						double margin = options.Margin;
						double yPoint = margin;
						double pageHeight = page.Height.Point;
						double pageWidth = page.Width.Point;
						double printableWidth = pageWidth - 2 * margin;
						double lineSpacing = options.FontSize * 1.2;

						var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
						foreach (var line in lines)
						{
							cancellationToken.ThrowIfCancellationRequested();

							if (line == "\f" || line.Contains("\f"))
							{
								page = document.AddPage();
								gfx.Dispose();
								gfx = XGraphics.FromPdfPage(page);
								yPoint = margin;
								continue;
							}

							var wrappedLines = WrapText(gfx, line, font, printableWidth);
							foreach (var wrappedLine in wrappedLines)
							{
								cancellationToken.ThrowIfCancellationRequested();
								if (yPoint + options.FontSize > pageHeight - margin)
								{
									page = document.AddPage();
									gfx.Dispose();
									gfx = XGraphics.FromPdfPage(page);
									yPoint = margin;
								}

								gfx.DrawString(wrappedLine, font, XBrushes.Black, margin, yPoint + options.FontSize);
								yPoint += lineSpacing;
							}
						}

						gfx.Dispose();
						document.Save(outputStream);
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to convert text to PDF.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> ImageToPdfAsync(IEnumerable<Stream> imageStreams, ImageToPdfOptions options, CancellationToken cancellationToken = default)
		{
			if (imageStreams == null) throw new ArgumentNullException(nameof(imageStreams));
			if (options == null) throw new ArgumentNullException(nameof(options));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					using (var document = new PdfDocument())
					{
						foreach (var imgStream in imageStreams)
						{
							cancellationToken.ThrowIfCancellationRequested();
							var seekable = EnsureSeekable(imgStream);
							try
							{
								var page = document.AddPage();
								using (var image = XImage.FromStream(seekable))
								{
									if (options.AutoPageSize)
									{
										page.Width = XUnit.FromPoint(image.PointWidth);
										page.Height = XUnit.FromPoint(image.PointHeight);
										using var gfx = XGraphics.FromPdfPage(page);
										gfx.DrawImage(image, 0, 0, image.PointWidth, image.PointHeight);
									}
									else
									{
										double margin = options.Margin;
										double pageWidth = page.Width.Point;
										double pageHeight = page.Height.Point;
										double printableWidth = pageWidth - 2 * margin;
										double printableHeight = pageHeight - 2 * margin;

										double imgWidth = image.PointWidth;
										double imgHeight = image.PointHeight;

										double scale = Math.Min(printableWidth / imgWidth, printableHeight / imgHeight);
										scale = Math.Min(scale, 1.0); // Don't upscale

										double drawWidth = imgWidth * scale;
										double drawHeight = imgHeight * scale;

										double x = margin + (printableWidth - drawWidth) / 2.0;
										double y = margin + (printableHeight - drawHeight) / 2.0;

										using var gfx = XGraphics.FromPdfPage(page);
										gfx.DrawImage(image, x, y, drawWidth, drawHeight);
									}
								}
							}
							finally
							{
								if (seekable != imgStream)
								{
									seekable.Dispose();
								}
							}
						}
						document.Save(outputStream);
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to convert images to PDF.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<PdfMetadata> GetMetadataAsync(Stream pdfStream, string? password = null, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));

			return await Task.Run(() =>
			{
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using (var document = string.IsNullOrEmpty(password)
							? PdfReader.Open(seekable, PdfDocumentOpenMode.Import)
							: PdfReader.Open(seekable, password, PdfDocumentOpenMode.Import))
						{
							cancellationToken.ThrowIfCancellationRequested();
							return new PdfMetadata
							{
								Title = document.Info.Title,
								Author = document.Info.Author,
								Subject = document.Info.Subject,
								Keywords = document.Info.Keywords,
								Creator = document.Info.Creator,
								Producer = document.Info.Producer,
								CreationDate = document.Info.CreationDate != DateTime.MinValue ? document.Info.CreationDate : (DateTime?)null,
								ModificationDate = document.Info.ModificationDate != DateTime.MinValue ? document.Info.ModificationDate : (DateTime?)null
							};
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					throw new PdfProcessingException("Failed to read PDF metadata.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> SetMetadataAsync(Stream pdfStream, PdfMetadata metadata, string? password = null, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (metadata == null) throw new ArgumentNullException(nameof(metadata));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using (var document = string.IsNullOrEmpty(password)
							? PdfReader.Open(seekable, PdfDocumentOpenMode.Modify)
							: PdfReader.Open(seekable, password, PdfDocumentOpenMode.Modify))
						{
							cancellationToken.ThrowIfCancellationRequested();

							document.Info.Title = metadata.Title ?? string.Empty;
							document.Info.Author = metadata.Author ?? string.Empty;
							document.Info.Subject = metadata.Subject ?? string.Empty;
							document.Info.Keywords = metadata.Keywords ?? string.Empty;
							document.Info.Creator = metadata.Creator ?? string.Empty;

							if (metadata.CreationDate.HasValue)
							{
								document.Info.CreationDate = metadata.CreationDate.Value;
							}
							if (metadata.ModificationDate.HasValue)
							{
								document.Info.ModificationDate = metadata.ModificationDate.Value;
							}

							document.Save(outputStream);
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to set PDF metadata.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> ProtectAsync(Stream pdfStream, ProtectionOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using (var document = PdfReader.Open(seekable, PdfDocumentOpenMode.Modify))
						{
							cancellationToken.ThrowIfCancellationRequested();

							var securitySettings = document.SecuritySettings;
							securitySettings.UserPassword = options.UserPassword ?? string.Empty;
							securitySettings.OwnerPassword = options.OwnerPassword ?? string.Empty;

							securitySettings.PermitPrint = options.PermitPrint;
							securitySettings.PermitModifyDocument = options.PermitModify;
							securitySettings.PermitExtractContent = options.PermitCopy;

							document.Save(outputStream);
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to protect PDF document.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> UnprotectAsync(Stream pdfStream, string password, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (password == null) throw new ArgumentNullException(nameof(password));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using (var document = PdfReader.Open(seekable, password, PdfDocumentOpenMode.Modify))
						{
							cancellationToken.ThrowIfCancellationRequested();

							document.SecurityHandler.SetEncryptionToNoneAndResetPasswords();

							document.Save(outputStream);
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to decrypt/unprotect PDF document.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> AddWatermarkAsync(Stream pdfStream, WatermarkOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					RegisterFontResolver();
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using (var document = string.IsNullOrEmpty(options.Password)
							? PdfReader.Open(seekable, PdfDocumentOpenMode.Modify)
							: PdfReader.Open(seekable, options.Password, PdfDocumentOpenMode.Modify))
						{
							cancellationToken.ThrowIfCancellationRequested();

							if (document.Version < 14)
							{
								document.Version = 14;
							}

							var font = new XFont(options.FontName, options.FontSize, XFontStyleEx.Bold);
							double opacity = Math.Max(0.0, Math.Min(1.0, options.Opacity));
							var brush = new XSolidBrush(XColor.FromArgb((int)(opacity * 255), 128, 128, 128));

							var format = new XStringFormat
							{
								Alignment = XStringAlignment.Center,
								LineAlignment = XLineAlignment.Center
							};

							int count = document.PageCount;
							for (int i = 0; i < count; i++)
							{
								cancellationToken.ThrowIfCancellationRequested();
								var page = document.Pages[i];

								using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
								{
									double width = page.Width.Point;
									double height = page.Height.Point;

									gfx.TranslateTransform(width / 2.0, height / 2.0);
									gfx.RotateTransform(-options.Rotation);

									gfx.DrawString(options.Text, font, brush, new XPoint(0, 0), format);
								}
							}

							document.Save(outputStream);
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to add watermark to PDF.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> ExtractPagesAsync(Stream pdfStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (pageNumbers == null) throw new ArgumentNullException(nameof(pageNumbers));

			return await Task.Run(() =>
			{
				var outputStream = new MemoryStream();
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						using (var inputDocument = PdfReader.Open(seekable, PdfDocumentOpenMode.Import))
						{
							using (var outputDocument = new PdfDocument())
							{
								var totalPages = inputDocument.PageCount;
								foreach (var pageNum in pageNumbers)
								{
									cancellationToken.ThrowIfCancellationRequested();
									if (pageNum >= 1 && pageNum <= totalPages)
									{
										var page = inputDocument.Pages[pageNum - 1];
										outputDocument.AddPage(page);
									}
									else
									{
										throw new PdfProcessingException($"Page number {pageNum} is out of bounds. The PDF has only {totalPages} pages.");
									}
								}

								if (outputDocument.PageCount == 0)
								{
									throw new PdfProcessingException("Cannot extract 0 pages. At least 1 page must be extracted.");
								}

								outputDocument.Save(outputStream);
							}
						}
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
					outputStream.Position = 0;
					return (Stream)outputStream;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is PdfProcessingException))
				{
					outputStream.Dispose();
					throw new PdfProcessingException("Failed to extract pages from PDF document.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		private static void RegisterFontResolver()
		{
			if (GlobalFontSettings.FontResolver == null)
			{
				try
				{
					GlobalFontSettings.FontResolver = new FallbackFontResolver();
				}
				catch (InvalidOperationException)
				{
					// Catch race conditions where another thread set it first.
				}
			}
		}

		private static Stream EnsureSeekable(Stream stream) => Helpers.StreamHelpers.EnsureSeekable(stream);

		private static List<List<int>> ParseRanges(string rangesStr, int totalPages)
		{
			var result = new List<List<int>>();
			var parts = rangesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var part in parts)
			{
				var trimmed = part.Trim();
				if (trimmed.Contains("-"))
				{
					var bounds = trimmed.Split('-');
					if (bounds.Length == 2 && int.TryParse(bounds[0], out int start) && int.TryParse(bounds[1], out int end))
					{
						start = Math.Max(1, Math.Min(start, totalPages));
						end = Math.Max(1, Math.Min(end, totalPages));
						var pageList = new List<int>();
						if (start <= end)
						{
							for (int i = start; i <= end; i++) pageList.Add(i);
						}
						else
						{
							for (int i = start; i >= end; i--) pageList.Add(i);
						}
						result.Add(pageList);
					}
				}
				else if (int.TryParse(trimmed, out int pageNum))
				{
					if (pageNum >= 1 && pageNum <= totalPages)
					{
						result.Add(new List<int> { pageNum });
					}
				}
			}
			return result;
		}

		private static List<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
		{
			var result = new List<string>();
			if (string.IsNullOrEmpty(text))
			{
				result.Add(string.Empty);
				return result;
			}

			if (gfx.MeasureString(text, font).Width <= maxWidth)
			{
				result.Add(text);
				return result;
			}

			var words = text.Split(' ');
			var currentLine = "";
			foreach (var word in words)
			{
				var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
				if (gfx.MeasureString(testLine, font).Width > maxWidth)
				{
					if (!string.IsNullOrEmpty(currentLine))
					{
						result.Add(currentLine);
						currentLine = word;
					}
					else
					{
						result.Add(word);
						currentLine = "";
					}
				}
				else
				{
					currentLine = testLine;
				}
			}

			if (!string.IsNullOrEmpty(currentLine))
			{
				result.Add(currentLine);
			}

			return result;
		}

		private static void OptimizeImagesInDocument(PdfDocument document, int quality, int maxDimension)
		{
			var objects = document.Internals.GetAllObjects();
			foreach (var obj in objects)
			{
				if (obj is PdfDictionary dict)
				{
					var subtype = dict.Elements.GetName("/Subtype");
					if (subtype == "/Image" || subtype == "Image")
					{
						try
						{
							var stream = dict.Stream;
							if (stream == null) continue;

							byte[]? unfilteredBytes = stream.UnfilteredValue;
							if (unfilteredBytes == null || unfilteredBytes.Length == 0) continue;

							int width = dict.Elements.GetInteger("/Width");
							int height = dict.Elements.GetInteger("/Height");
							if (width <= 0 || height <= 0) continue;

							using var image = LoadImageFromPdfStream(dict, unfilteredBytes, width, height);
							if (image == null) continue;

							SKBitmap finalBitmap = image;
							bool wasResized = false;

							// Downscale if it's too large (based on maxDimension parameter)
							if (maxDimension > 0 && (image.Width > maxDimension || image.Height > maxDimension))
							{
								double ratio = (double)maxDimension / Math.Max(image.Width, image.Height);
								int newWidth = (int)(image.Width * ratio);
								int newHeight = (int)(image.Height * ratio);
								var resized = image.Resize(new SKImageInfo(newWidth, newHeight), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
								if (resized != null)
								{
									finalBitmap = resized;
									wasResized = true;
								}
							}

							try
							{
								using var outMs = new MemoryStream();
								using var skImage = SKImage.FromBitmap(finalBitmap);
								using var encodedData = skImage.Encode(SKEncodedImageFormat.Jpeg, quality);
								encodedData.SaveTo(outMs);
								byte[] jpegBytes = outMs.ToArray();

								if (jpegBytes.Length < stream.Value.Length)
								{
									dict.Elements["/Filter"] = new PdfName("/DCTDecode");
									dict.Elements["/Width"] = new PdfInteger(finalBitmap.Width);
									dict.Elements["/Height"] = new PdfInteger(finalBitmap.Height);
									dict.Elements["/ColorSpace"] = new PdfName("/DeviceRGB");
									dict.Elements.Remove("/DecodeParms");

									stream.Value = jpegBytes;
									dict.Elements["/Length"] = new PdfInteger(jpegBytes.Length);
								}
							}
							finally
							{
								if (wasResized)
								{
									finalBitmap.Dispose();
								}
							}
						}
						catch
						{
							// Keep original image if processing fails
						}
					}
				}
			}
		}

		private static SKBitmap? LoadImageFromPdfStream(PdfDictionary dict, byte[] unfilteredBytes, int width, int height)
		{
			var filter = dict.Elements.GetName("/Filter");

			if (filter == "/DCTDecode" || filter == "DCTDecode")
			{
				return SKBitmap.Decode(unfilteredBytes);
			}

			var bitsPerComponent = dict.Elements.GetInteger("/BitsPerComponent");
			if (bitsPerComponent != 8) return null;

			// Extract /DecodeParms and read /Predictor setting
			int predictor = 1;
			var decodeParms = dict.Elements["/DecodeParms"];
			if (decodeParms is PdfReference parmsRef)
			{
				decodeParms = parmsRef.Value;
			}

			if (decodeParms is PdfDictionary parmsDict)
			{
				predictor = parmsDict.Elements.GetInteger("/Predictor");
				if (predictor == 0) predictor = 1;
			}
			else if (decodeParms is PdfArray parmsArray)
			{
				foreach (var item in parmsArray)
				{
					var actualItem = item;
					if (actualItem is PdfReference itemRef)
					{
						actualItem = itemRef.Value;
					}
					if (actualItem is PdfDictionary pDict)
					{
						int pred = pDict.Elements.GetInteger("/Predictor");
						if (pred > 1)
						{
							predictor = pred;
							break;
						}
					}
				}
			}

			// Determine if indexed colorspace is used
			PdfItem? colorSpaceObj = dict.Elements["/ColorSpace"];
			if (colorSpaceObj is PdfReference csRef)
			{
				colorSpaceObj = csRef.Value;
			}

			bool isIndexed = false;
			PdfItem? baseColorSpace = null;
			int hival = 0;
			PdfItem? lookupTable = null;

			if (colorSpaceObj is PdfName csName)
			{
				isIndexed = csName.Value == "/Indexed" || csName.Value == "Indexed";
			}
			else if (colorSpaceObj is PdfArray csArray && csArray.Elements.Count > 0)
			{
				var first = csArray.Elements[0] as PdfName;
				if (first != null && (first.Value == "/Indexed" || first.Value == "Indexed"))
				{
					isIndexed = true;
					if (csArray.Elements.Count > 3)
					{
						baseColorSpace = csArray.Elements[1];
						if (csArray.Elements[2] is PdfInteger hiInt)
						{
							hival = hiInt.Value;
						}
						lookupTable = csArray.Elements[3];
					}
				}
			}

			string colorSpace = "";
			if (colorSpaceObj is PdfName csNameDirect)
			{
				colorSpace = csNameDirect.Value;
			}

			int colorsCount = 1;
			if (isIndexed)
			{
				colorsCount = 1;
			}
			else if (colorSpace == "/DeviceRGB" || colorSpace == "DeviceRGB")
			{
				colorsCount = 3;
			}
			else if (colorSpace == "/DeviceGray" || colorSpace == "DeviceGray")
			{
				colorsCount = 1;
			}
			else if (colorSpace == "/DeviceCMYK" || colorSpace == "DeviceCMYK")
			{
				colorsCount = 4;
			}

			// Decode PNG predictor if applicable
			byte[] processedBytes = unfilteredBytes;
			if (predictor >= 10 && predictor <= 15)
			{
				processedBytes = DecodePngPredictor(unfilteredBytes, width, height, colorsCount, bitsPerComponent);
			}

			// Decode indexed color space
			if (isIndexed && lookupTable != null)
			{
				if (lookupTable is PdfReference lookupRef)
				{
					lookupTable = lookupRef.Value;
				}

				byte[]? lookupBytes = null;
				if (lookupTable is PdfString pdfStr)
				{
					lookupBytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(pdfStr.Value);
				}
				else if (lookupTable is PdfDictionary lookupDict && lookupDict.Stream != null)
				{
					lookupBytes = lookupDict.Stream.UnfilteredValue;
				}

				if (lookupBytes != null)
				{
					int baseComponents = 3;
					bool baseIsCmyk = false;
					bool baseIsGray = false;

					if (baseColorSpace is PdfReference baseRef)
					{
						baseColorSpace = baseRef.Value;
					}

					if (baseColorSpace is PdfName baseName)
					{
						if (baseName.Value == "/DeviceGray" || baseName.Value == "DeviceGray")
						{
							baseComponents = 1;
							baseIsGray = true;
						}
						else if (baseName.Value == "/DeviceCMYK" || baseName.Value == "DeviceCMYK")
						{
							baseComponents = 4;
							baseIsCmyk = true;
						}
					}

					if (bitsPerComponent == 8 && processedBytes.Length >= width * height)
					{
						byte[] rgbBytes = new byte[width * height * 3];
						for (int i = 0; i < width * height; i++)
						{
							int index = processedBytes[i];
							int lookupOffset = index * baseComponents;
							int dstIdx = i * 3;

							if (lookupOffset + baseComponents <= lookupBytes.Length)
							{
								if (baseIsCmyk)
								{
									int c = lookupBytes[lookupOffset];
									int m = lookupBytes[lookupOffset + 1];
									int y = lookupBytes[lookupOffset + 2];
									int k = lookupBytes[lookupOffset + 3];
									rgbBytes[dstIdx] = (byte)((255 - c) * (255 - k) / 255);
									rgbBytes[dstIdx + 1] = (byte)((255 - m) * (255 - k) / 255);
									rgbBytes[dstIdx + 2] = (byte)((255 - y) * (255 - k) / 255);
								}
								else if (baseIsGray)
								{
									byte g = lookupBytes[lookupOffset];
									rgbBytes[dstIdx] = g;
									rgbBytes[dstIdx + 1] = g;
									rgbBytes[dstIdx + 2] = g;
								}
								else
								{
									rgbBytes[dstIdx] = lookupBytes[lookupOffset];
									rgbBytes[dstIdx + 1] = lookupBytes[lookupOffset + 1];
									rgbBytes[dstIdx + 2] = lookupBytes[lookupOffset + 2];
								}
							}
						}
						return CreateBitmapFromRgb24(rgbBytes, width, height);
					}
				}
			}

			// Decode standard color spaces
			if (colorSpace == "/DeviceRGB" || colorSpace == "DeviceRGB")
			{
				if (processedBytes.Length >= width * height * 3)
				{
					return CreateBitmapFromRgb24(processedBytes, width, height);
				}
			}
			else if (colorSpace == "/DeviceGray" || colorSpace == "DeviceGray")
			{
				if (processedBytes.Length >= width * height)
				{
					return CreateBitmapFromL8(processedBytes, width, height);
				}
			}
			else if (colorSpace == "/DeviceCMYK" || colorSpace == "DeviceCMYK")
			{
				if (processedBytes.Length >= width * height * 4)
				{
					byte[] rgbBytes = new byte[width * height * 3];
					for (int i = 0; i < width * height; i++)
					{
						int srcIdx = i * 4;
						int dstIdx = i * 3;

						int c = processedBytes[srcIdx];
						int m = processedBytes[srcIdx + 1];
						int y = processedBytes[srcIdx + 2];
						int k = processedBytes[srcIdx + 3];

						rgbBytes[dstIdx] = (byte)((255 - c) * (255 - k) / 255);
						rgbBytes[dstIdx + 1] = (byte)((255 - m) * (255 - k) / 255);
						rgbBytes[dstIdx + 2] = (byte)((255 - y) * (255 - k) / 255);
					}
					return CreateBitmapFromRgb24(rgbBytes, width, height);
				}
			}

			return null;
		}

		private static SKBitmap CreateBitmapFromRgb24(byte[] rgbBytes, int width, int height)
		{
			byte[] bgraBytes = new byte[width * height * 4];
			for (int i = 0; i < width * height; i++)
			{
				int srcIdx = i * 3;
				int dstIdx = i * 4;
				bgraBytes[dstIdx] = rgbBytes[srcIdx + 2];     // B
				bgraBytes[dstIdx + 1] = rgbBytes[srcIdx + 1]; // G
				bgraBytes[dstIdx + 2] = rgbBytes[srcIdx];     // R
				bgraBytes[dstIdx + 3] = 255;                  // A
			}
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			var bitmap = new SKBitmap(info);
			System.Runtime.InteropServices.Marshal.Copy(bgraBytes, 0, bitmap.GetPixels(), bgraBytes.Length);
			return bitmap;
		}

		private static SKBitmap CreateBitmapFromL8(byte[] grayBytes, int width, int height)
		{
			byte[] bgraBytes = new byte[width * height * 4];
			for (int i = 0; i < width * height; i++)
			{
				byte g = grayBytes[i];
				int dstIdx = i * 4;
				bgraBytes[dstIdx] = g;     // B
				bgraBytes[dstIdx + 1] = g; // G
				bgraBytes[dstIdx + 2] = g; // R
				bgraBytes[dstIdx + 3] = 255; // A
			}
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			var bitmap = new SKBitmap(info);
			System.Runtime.InteropServices.Marshal.Copy(bgraBytes, 0, bitmap.GetPixels(), bgraBytes.Length);
			return bitmap;
		}

		private static byte[] DecodePngPredictor(byte[] data, int width, int height, int colors, int bitsPerComponent)
		{
			if (bitsPerComponent != 8)
				return data; // Currently we only support 8-bit components for de-prediction

			int bytesPerPixel = colors;
			int rowStride = width * bytesPerPixel;
			int srcStride = 1 + rowStride; // 1 byte for predictor type + row data

			if (data.Length < height * srcStride)
			{
				// Data is truncated or not matching expected stride, return as is
				return data;
			}

			byte[] decoded = new byte[height * rowStride];
			byte[] prevRow = new byte[rowStride]; // initialized to 0
			byte[] currRow = new byte[rowStride];

			for (int y = 0; y < height; y++)
			{
				int srcOffset = y * srcStride;
				int dstOffset = y * rowStride;

				if (srcOffset >= data.Length) break;

				byte predictorType = data[srcOffset];

				int bytesToCopy = Math.Min(rowStride, data.Length - (srcOffset + 1));
				if (bytesToCopy <= 0) break;
				Array.Copy(data, srcOffset + 1, currRow, 0, bytesToCopy);
				if (bytesToCopy < rowStride)
				{
					Array.Clear(currRow, bytesToCopy, rowStride - bytesToCopy);
				}

				switch (predictorType)
				{
					case 0: // None
						break;
					case 1: // Sub
						for (int x = 0; x < rowStride; x++)
						{
							int leftIdx = x - bytesPerPixel;
							byte left = leftIdx >= 0 ? currRow[leftIdx] : (byte)0;
							currRow[x] = (byte)((currRow[x] + left) & 0xFF);
						}
						break;
					case 2: // Up
						for (int x = 0; x < rowStride; x++)
						{
							byte up = prevRow[x];
							currRow[x] = (byte)((currRow[x] + up) & 0xFF);
						}
						break;
					case 3: // Average
						for (int x = 0; x < rowStride; x++)
						{
							int leftIdx = x - bytesPerPixel;
							byte left = leftIdx >= 0 ? currRow[leftIdx] : (byte)0;
							byte up = prevRow[x];
							currRow[x] = (byte)((currRow[x] + (left + up) / 2) & 0xFF);
						}
						break;
					case 4: // Paeth
						for (int x = 0; x < rowStride; x++)
						{
							int leftIdx = x - bytesPerPixel;
							byte a = leftIdx >= 0 ? currRow[leftIdx] : (byte)0;
							byte b = prevRow[x];
							byte c = leftIdx >= 0 ? prevRow[leftIdx] : (byte)0;

							int p = a + b - c;
							int pa = Math.Abs(p - a);
							int pb = Math.Abs(p - b);
							int pc = Math.Abs(p - c);

							byte paeth;
							if (pa <= pb && pa <= pc) paeth = a;
							else if (pb <= pc) paeth = b;
							else paeth = c;

							currRow[x] = (byte)((currRow[x] + paeth) & 0xFF);
						}
						break;
					default:
						// Leave as is if unknown type
						break;
				}

				Array.Copy(currRow, 0, decoded, dstOffset, rowStride);
				Array.Copy(currRow, 0, prevRow, 0, rowStride);
			}

			return decoded;
		}

		private static void DeduplicateImages(PdfDocument document)
		{
			var seenImages = new Dictionary<string, PdfReference>();

			foreach (var page in document.Pages)
			{
				var resources = page.Elements.GetDictionary("/Resources");
				if (resources == null) continue;

				var xObjects = resources.Elements.GetDictionary("/XObject");
				if (xObjects == null) continue;

				var keys = xObjects.Elements.Keys.ToList();
				foreach (var key in keys)
				{
					var item = xObjects.Elements[key];
					if (item is PdfReference reference && reference.Value is PdfDictionary dict)
					{
						var subtype = dict.Elements.GetName("/Subtype");
						if (subtype == "/Image" || subtype == "Image")
						{
							var stream = dict.Stream;
							if (stream == null) continue;

							byte[]? rawBytes = stream.Value;
							if (rawBytes == null || rawBytes.Length == 0) continue;

							string hash = GetFastHash(rawBytes);

							if (seenImages.TryGetValue(hash, out var firstRef))
							{
								// Redirect this key to the first reference to deduplicate
								xObjects.Elements[key] = firstRef;
							}
							else
							{
								seenImages[hash] = reference;
							}
						}
					}
				}
			}
		}

		private static string GetFastHash(byte[] bytes)
		{
			// FIPS-compliant fast FNV-1a 64-bit hashing
			unchecked
			{
				ulong hash = 14695981039346656037;
				for (int i = 0; i < bytes.Length; i++)
				{
					hash ^= bytes[i];
					hash *= 1099511628211;
				}
				return hash.ToString("X16");
			}
		}

		private static void OptimizePdfStructure(PdfDocument document, bool removeMetadata)
		{
			// Remove document-level metadata and private data
			if (removeMetadata)
			{
				document.Info.Title = "";
				document.Info.Author = "";
				document.Info.Subject = "";
				document.Info.Keywords = "";
				document.Info.Creator = "";

				document.Internals.Catalog.Elements.Remove("/Metadata");
				document.Internals.Catalog.Elements.Remove("/StructTreeRoot");
				document.Internals.Catalog.Elements.Remove("/PieceInfo");
			}

			var objects = document.Internals.GetAllObjects();
			foreach (var obj in objects)
			{
				if (obj is PdfDictionary dict)
				{
					// Losslessly remove private application data (e.g. Illustrator preservation data)
					dict.Elements.Remove("/PieceInfo");

					if (removeMetadata)
					{
						// Remove object-level metadata streams
						dict.Elements.Remove("/Metadata");
					}
				}
			}

			// Remove page thumbnails (viewers generate them on the fly)
			foreach (var page in document.Pages)
			{
				page.Elements.Remove("/Thumb");
				page.Elements.Remove("/PieceInfo");
				if (removeMetadata)
				{
					page.Elements.Remove("/Metadata");
				}
			}
		}
	}
}
