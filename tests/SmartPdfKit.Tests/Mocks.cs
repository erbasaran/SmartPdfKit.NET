using SmartPdfKit.Interfaces;
using SmartPdfKit.Models;

namespace SmartPdfKit.Tests
{
	public class MockPdfProcessor : IPdfProcessor
	{
		public bool MergeCalled { get; private set; }
		public bool SplitCalled { get; private set; }
		public bool CompressCalled { get; private set; }
		public bool RotateCalled { get; private set; }
		public bool CropCalled { get; private set; }
		public bool RemovePagesCalled { get; private set; }
		public bool TextToPdfCalled { get; private set; }
		public bool ImageToPdfCalled { get; private set; }

		public Task<Stream> MergeAsync(IEnumerable<Stream> pdfStreams, CancellationToken cancellationToken = default)
		{
			MergeCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public Task<IEnumerable<Stream>> SplitAsync(Stream pdfStream, SplitOptions options, CancellationToken cancellationToken = default)
		{
			SplitCalled = true;
			return Task.FromResult<IEnumerable<Stream>>(new List<Stream> { new MemoryStream() });
		}

		public Task<Stream> CompressAsync(Stream pdfStream, CompressOptions options, CancellationToken cancellationToken = default)
		{
			CompressCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public Task<Stream> RotateAsync(Stream pdfStream, int rotationDegrees, CancellationToken cancellationToken = default)
		{
			RotateCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public Task<Stream> CropAsync(Stream pdfStream, CropOptions options, CancellationToken cancellationToken = default)
		{
			CropCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public Task<Stream> RemovePagesAsync(Stream pdfStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
		{
			RemovePagesCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public Task<Stream> TextToPdfAsync(string text, TextToPdfOptions options, CancellationToken cancellationToken = default)
		{
			TextToPdfCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public Task<Stream> ImageToPdfAsync(IEnumerable<Stream> imageStreams, ImageToPdfOptions options, CancellationToken cancellationToken = default)
		{
			ImageToPdfCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public bool GetMetadataCalled { get; private set; }
		public bool SetMetadataCalled { get; private set; }
		public bool ProtectCalled { get; private set; }
		public bool UnprotectCalled { get; private set; }
		public bool AddWatermarkCalled { get; private set; }
		public bool ExtractPagesCalled { get; private set; }

		public Task<PdfMetadata> GetMetadataAsync(Stream pdfStream, string? password = null, CancellationToken cancellationToken = default)
		{
			GetMetadataCalled = true;
			return Task.FromResult(new PdfMetadata { Title = "Mock Title", Author = "Mock Author" });
		}

		public Task<Stream> SetMetadataAsync(Stream pdfStream, PdfMetadata metadata, string? password = null, CancellationToken cancellationToken = default)
		{
			SetMetadataCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public Task<Stream> ProtectAsync(Stream pdfStream, ProtectionOptions options, CancellationToken cancellationToken = default)
		{
			ProtectCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public Task<Stream> UnprotectAsync(Stream pdfStream, string password, CancellationToken cancellationToken = default)
		{
			UnprotectCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public Task<Stream> AddWatermarkAsync(Stream pdfStream, WatermarkOptions options, CancellationToken cancellationToken = default)
		{
			AddWatermarkCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}

		public Task<Stream> ExtractPagesAsync(Stream pdfStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
		{
			ExtractPagesCalled = true;
			return Task.FromResult<Stream>(new MemoryStream());
		}
	}

	public class MockPdfRenderer : IPdfRenderer
	{
		public bool RenderToImagesCalled { get; private set; }

		public Task<IEnumerable<Stream>> RenderToImagesAsync(Stream pdfStream, ImageConversionOptions options, CancellationToken cancellationToken = default)
		{
			RenderToImagesCalled = true;
			return Task.FromResult<IEnumerable<Stream>>(new List<Stream> { new MemoryStream() });
		}
	}

	public class MockPdfTextExtractor : IPdfTextExtractor
	{
		public bool ExtractTextCalled { get; private set; }
		public string TextToReturn { get; set; } = "Mock Text Content";

		public Task<string> ExtractTextAsync(Stream pdfStream, TextExtractionOptions options, CancellationToken cancellationToken = default)
		{
			ExtractTextCalled = true;
			return Task.FromResult(TextToReturn);
		}
	}

	public class MockOcrProvider : IOcrProvider
	{
		public bool PerformOcrCalled { get; private set; }
		public string TextToReturn { get; set; } = "OCR Mock Text";
		public float ConfidenceToReturn { get; set; } = 0.95f;

		public Task<OcrResult> PerformOcrAsync(Stream pdfStream, OcrOptions options, CancellationToken cancellationToken = default)
		{
			PerformOcrCalled = true;
			var ms = options.Mode == OcrMode.SearchablePdf ? new MemoryStream() : null;
			return Task.FromResult(new OcrResult(TextToReturn, ConfidenceToReturn, ms));
		}
	}

	public class MockLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
		public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
		public void Log<TState>(
			Microsoft.Extensions.Logging.LogLevel logLevel,
			Microsoft.Extensions.Logging.EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			// Do nothing
		}
	}
}
