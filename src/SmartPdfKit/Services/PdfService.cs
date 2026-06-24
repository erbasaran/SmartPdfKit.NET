using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartPdfKit.Exceptions;
using SmartPdfKit.Interfaces;
using SmartPdfKit.Models;

namespace SmartPdfKit.Services
{
	/// <summary>
	/// Coordinates PDF editing, rendering, extraction, and OCR tasks by delegating to specialized providers.
	/// </summary>
	public class PdfService : IPdfService
	{
		private readonly IPdfProcessor _pdfProcessor;
		private readonly IPdfRenderer _pdfRenderer;
		private readonly IPdfTextExtractor _pdfTextExtractor;
		private readonly IOcrProvider? _ocrProvider;
		private readonly ILogger<PdfService> _logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="PdfService"/> class.
		/// </summary>
		/// <param name="pdfProcessor">The PDF editor engine.</param>
		/// <param name="pdfRenderer">The PDF image rendering engine.</param>
		/// <param name="pdfTextExtractor">The PDF text extraction engine.</param>
		/// <param name="logger">The logger instance.</param>
		/// <param name="ocrProvider">The optional OCR processor engine.</param>
		public PdfService(
			IPdfProcessor pdfProcessor,
			IPdfRenderer pdfRenderer,
			IPdfTextExtractor pdfTextExtractor,
			ILogger<PdfService> logger,
			IOcrProvider? ocrProvider = null)
		{
			_pdfProcessor = pdfProcessor ?? throw new ArgumentNullException(nameof(pdfProcessor));
			_pdfRenderer = pdfRenderer ?? throw new ArgumentNullException(nameof(pdfRenderer));
			_pdfTextExtractor = pdfTextExtractor ?? throw new ArgumentNullException(nameof(pdfTextExtractor));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_ocrProvider = ocrProvider;
		}

		/// <inheritdoc/>
		public async Task<Stream> MergeAsync(IEnumerable<Stream> pdfStreams, CancellationToken cancellationToken = default)
		{
			if (pdfStreams == null) throw new ArgumentNullException(nameof(pdfStreams));

			var streamList = pdfStreams.ToList();
			if (streamList.Count == 0)
			{
				throw new ArgumentException("The list of streams to merge cannot be empty.", nameof(pdfStreams));
			}

			foreach (var stream in streamList)
			{
				if (stream == null)
				{
					throw new ArgumentException("Stream collection contains a null reference.", nameof(pdfStreams));
				}
				if (!stream.CanRead)
				{
					throw new ArgumentException("One or more streams are unreadable.", nameof(pdfStreams));
				}
			}

			_logger.LogInformation("Merging {Count} PDF streams.", streamList.Count);
			return await _pdfProcessor.MergeAsync(streamList, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<IEnumerable<Stream>> SplitAsync(Stream pdfStream, SplitOptions? options = null, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			options ??= new SplitOptions();
			if (options.Mode == SplitMode.SplitFixedSize && options.PageInterval <= 0)
			{
				throw new ArgumentException("Page interval must be greater than zero for fixed-size splitting.", nameof(options));
			}
			if (options.Mode == SplitMode.SplitByRanges && string.IsNullOrWhiteSpace(options.Ranges))
			{
				throw new ArgumentException("Ranges must be specified when using range-based splitting.", nameof(options));
			}

			_logger.LogInformation("Splitting PDF stream with mode {Mode}.", options.Mode);
			return await _pdfProcessor.SplitAsync(pdfStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> CompressAsync(Stream pdfStream, CompressOptions? options = null, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			options ??= new CompressOptions();
			_logger.LogInformation("Compressing PDF stream.");
			return await _pdfProcessor.CompressAsync(pdfStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> RotateAsync(Stream pdfStream, int rotationDegrees, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			int normalized = ((rotationDegrees % 360) + 360) % 360;
			if (normalized != 90 && normalized != 180 && normalized != 270 && normalized != 0)
			{
				throw new ArgumentException("Rotation degrees must be a multiple of 90 (e.g. 90, 180, 270).", nameof(rotationDegrees));
			}

			_logger.LogInformation("Rotating PDF stream by {Degrees} degrees.", rotationDegrees);
			return await _pdfProcessor.RotateAsync(pdfStream, rotationDegrees, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> CropAsync(Stream pdfStream, CropOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			if (options.Left < 0 || options.Top < 0 || options.Right < 0 || options.Bottom < 0)
			{
				throw new ArgumentException("Crop dimensions cannot be negative.", nameof(options));
			}

			_logger.LogInformation("Cropping PDF stream. Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}", options.Left, options.Top, options.Right, options.Bottom);
			return await _pdfProcessor.CropAsync(pdfStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> RemovePagesAsync(Stream pdfStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (pageNumbers == null) throw new ArgumentNullException(nameof(pageNumbers));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			var pages = pageNumbers.ToList();
			if (pages.Count == 0)
			{
				throw new ArgumentException("The list of pages to remove cannot be empty.", nameof(pageNumbers));
			}
			if (pages.Any(p => p <= 0))
			{
				throw new ArgumentException("Page numbers must be positive, 1-based indices.", nameof(pageNumbers));
			}

			_logger.LogInformation("Removing pages {Pages} from PDF stream.", string.Join(", ", pages));
			return await _pdfProcessor.RemovePagesAsync(pdfStream, pages, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<OcrResult> OcrAsync(Stream pdfStream, OcrOptions? options = null, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			if (_ocrProvider == null)
			{
				throw new PdfOcrException("No OCR provider has been registered. Ensure that SmartPdfKit.Infrastructure is configured and Tesseract is registered.");
			}

			options ??= new OcrOptions();
			_logger.LogInformation("Performing OCR on PDF stream in language '{Language}'.", options.Language);
			return await _ocrProvider.PerformOcrAsync(pdfStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<IEnumerable<Stream>> ConvertToImagesAsync(Stream pdfStream, ImageConversionOptions? options = null, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			options ??= new ImageConversionOptions();
			if (options.Dpi <= 0)
			{
				throw new ArgumentException("DPI must be greater than zero.", nameof(options));
			}
			if (options.Quality < 0 || options.Quality > 100)
			{
				throw new ArgumentException("Quality must be between 0 and 100.", nameof(options));
			}

			_logger.LogInformation("Converting PDF stream to images (Format: {Format}, DPI: {Dpi}).", options.Format, options.Dpi);
			return await _pdfRenderer.RenderToImagesAsync(pdfStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> ConvertToPdfAsync(IEnumerable<Stream> imageStreams, ImageToPdfOptions? options = null, CancellationToken cancellationToken = default)
		{
			if (imageStreams == null) throw new ArgumentNullException(nameof(imageStreams));

			var streams = imageStreams.ToList();
			if (streams.Count == 0)
			{
				throw new ArgumentException("Image stream collection cannot be empty.", nameof(imageStreams));
			}

			foreach (var stream in streams)
			{
				if (stream == null)
				{
					throw new ArgumentException("Image stream collection contains a null reference.", nameof(imageStreams));
				}
				if (!stream.CanRead)
				{
					throw new ArgumentException("One or more image streams are unreadable.", nameof(imageStreams));
				}
			}

			options ??= new ImageToPdfOptions();
			_logger.LogInformation("Converting {Count} images to PDF.", streams.Count);
			return await _pdfProcessor.ImageToPdfAsync(streams, options, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<string> ConvertToTextAsync(Stream pdfStream, TextExtractionOptions? options = null, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			options ??= new TextExtractionOptions();
			_logger.LogInformation("Extracting text from PDF stream.");
			return await _pdfTextExtractor.ExtractTextAsync(pdfStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> ConvertTextToPdfAsync(string text, TextToPdfOptions? options = null, CancellationToken cancellationToken = default)
		{
			if (text == null) throw new ArgumentNullException(nameof(text));

			options ??= new TextToPdfOptions();
			_logger.LogInformation("Converting raw text to PDF.");
			return await _pdfProcessor.TextToPdfAsync(text, options, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<PdfMetadata> GetMetadataAsync(Stream pdfStream, string? password = null, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			_logger.LogInformation("Retrieving PDF document metadata.");
			return await _pdfProcessor.GetMetadataAsync(pdfStream, password, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> SetMetadataAsync(Stream pdfStream, PdfMetadata metadata, string? password = null, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (metadata == null) throw new ArgumentNullException(nameof(metadata));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			_logger.LogInformation("Updating PDF document metadata.");
			return await _pdfProcessor.SetMetadataAsync(pdfStream, metadata, password, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> ProtectAsync(Stream pdfStream, ProtectionOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			_logger.LogInformation("Encrypting and protecting PDF document.");
			return await _pdfProcessor.ProtectAsync(pdfStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> UnprotectAsync(Stream pdfStream, string password, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (password == null) throw new ArgumentNullException(nameof(password));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			_logger.LogInformation("Decrypting PDF document.");
			return await _pdfProcessor.UnprotectAsync(pdfStream, password, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> AddWatermarkAsync(Stream pdfStream, WatermarkOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			_logger.LogInformation("Applying text watermark '{Text}' to PDF.", options.Text);
			return await _pdfProcessor.AddWatermarkAsync(pdfStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<Stream> ExtractPagesAsync(Stream pdfStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (pageNumbers == null) throw new ArgumentNullException(nameof(pageNumbers));
			if (!pdfStream.CanRead) throw new ArgumentException("Stream is unreadable.", nameof(pdfStream));

			var pages = pageNumbers.ToList();
			if (pages.Count == 0)
			{
				throw new ArgumentException("The list of pages to extract cannot be empty.", nameof(pageNumbers));
			}
			if (pages.Any(p => p <= 0))
			{
				throw new ArgumentException("Page numbers must be positive, 1-based indices.", nameof(pageNumbers));
			}

			_logger.LogInformation("Extracting pages {Pages} from PDF stream.", string.Join(", ", pages));
			return await _pdfProcessor.ExtractPagesAsync(pdfStream, pages, cancellationToken).ConfigureAwait(false);
		}
	}
}
