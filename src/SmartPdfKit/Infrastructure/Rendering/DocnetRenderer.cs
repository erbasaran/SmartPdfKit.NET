using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Docnet.Core;
using Docnet.Core.Models;
using SkiaSharp;
using SmartPdfKit.Exceptions;
using SmartPdfKit.Interfaces;
using SmartPdfKit.Models;

namespace SmartPdfKit.Infrastructure.Rendering
{
	/// <summary>
	/// Implements <see cref="IPdfRenderer"/> using Docnet.Core (PDFium) and SkiaSharp to render PDF pages to images.
	/// </summary>
	public class DocnetRenderer : IPdfRenderer
	{
		/// <inheritdoc/>
		public async Task<IEnumerable<Stream>> RenderToImagesAsync(Stream pdfStream, ImageConversionOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));

			return await Task.Run(async () =>
			{
				var result = new List<Stream>();
				try
				{
					byte[] pdfBytes;
					if (pdfStream is MemoryStream msStream)
					{
						pdfBytes = msStream.ToArray();
					}
					else
					{
						using var ms = new MemoryStream();
						await pdfStream.CopyToAsync(ms, 81920, cancellationToken).ConfigureAwait(false);
						pdfBytes = ms.ToArray();
					}

					double scalingFactor = options.Dpi / 72.0;
					var pageDimensions = new PageDimensions(scalingFactor);

					using var docReader = string.IsNullOrEmpty(options.Password)
						? DocLib.Instance.GetDocReader(pdfBytes, pageDimensions)
						: DocLib.Instance.GetDocReader(pdfBytes, options.Password, pageDimensions);

					int pageCount = docReader.GetPageCount();
					for (int i = 0; i < pageCount; i++)
					{
						cancellationToken.ThrowIfCancellationRequested();
						using var pageReader = docReader.GetPageReader(i);

						byte[] rawBytes = pageReader.GetImage();
						int width = pageReader.GetPageWidth();
						int height = pageReader.GetPageHeight();

						var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
						using var bitmap = new SKBitmap();
						var gcHandle = System.Runtime.InteropServices.GCHandle.Alloc(rawBytes, System.Runtime.InteropServices.GCHandleType.Pinned);
						try
						{
							bitmap.InstallPixels(info, gcHandle.AddrOfPinnedObject(), info.RowBytes, delegate { gcHandle.Free(); });
						}
						catch
						{
							gcHandle.Free();
							throw;
						}

						using var image = SKImage.FromBitmap(bitmap);
						var imageMs = new MemoryStream();
						result.Add(imageMs);

						if (options.Format == ImageFormat.Png)
						{
							using var data = image.Encode(SKEncodedImageFormat.Png, 100);
							data.SaveTo(imageMs);
						}
						else
						{
							using var data = image.Encode(SKEncodedImageFormat.Jpeg, options.Quality);
							data.SaveTo(imageMs);
						}

						imageMs.Position = 0;
					}

					return (IEnumerable<Stream>)result;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					foreach (var s in result)
					{
						s.Dispose();
					}
					throw new PdfProcessingException("Failed to render PDF pages to images.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}
	}
}
