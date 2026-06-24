using SmartPdfKit.Exceptions;
using SmartPdfKit.Models;
using SmartPdfKit.Services;

namespace SmartPdfKit.Tests
{
	public class PdfServiceTests
	{
		private readonly MockPdfProcessor _processor;
		private readonly MockPdfRenderer _renderer;
		private readonly MockPdfTextExtractor _extractor;
		private readonly MockOcrProvider _ocrProvider;
		private readonly PdfService _service;

		public PdfServiceTests()
		{
			_processor = new MockPdfProcessor();
			_renderer = new MockPdfRenderer();
			_extractor = new MockPdfTextExtractor();
			_ocrProvider = new MockOcrProvider();
			_service = new PdfService(_processor, _renderer, _extractor, new MockLogger<PdfService>(), _ocrProvider);
		}

		[Fact]
		public async Task MergeAsync_NullStreams_ThrowsArgumentNullException()
		{
			await Assert.ThrowsAsync<ArgumentNullException>(() => _service.MergeAsync(null!));
		}

		[Fact]
		public async Task MergeAsync_EmptyStreams_ThrowsArgumentException()
		{
			var streams = new List<Stream>();
			await Assert.ThrowsAsync<ArgumentException>(() => _service.MergeAsync(streams));
		}

		[Fact]
		public async Task MergeAsync_UnreadableStream_ThrowsArgumentException()
		{
			var mockStream = new MemoryStream();
			mockStream.Close(); // Make it unreadable
			var streams = new List<Stream> { mockStream };
			await Assert.ThrowsAsync<ArgumentException>(() => _service.MergeAsync(streams));
		}

		[Fact]
		public async Task MergeAsync_ValidInputs_CallsProcessor()
		{
			var streams = new List<Stream> { new MemoryStream() };
			await _service.MergeAsync(streams);
			Assert.True(_processor.MergeCalled);
		}

		[Fact]
		public async Task SplitAsync_NullStream_ThrowsArgumentNullException()
		{
			await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SplitAsync(null!));
		}

		[Fact]
		public async Task SplitAsync_FixedSizeWithInvalidInterval_ThrowsArgumentException()
		{
			var stream = new MemoryStream();
			var options = new SplitOptions { Mode = SplitMode.SplitFixedSize, PageInterval = 0 };
			await Assert.ThrowsAsync<ArgumentException>(() => _service.SplitAsync(stream, options));
		}

		[Fact]
		public async Task SplitAsync_RangesWithNullRangeString_ThrowsArgumentException()
		{
			var stream = new MemoryStream();
			var options = new SplitOptions { Mode = SplitMode.SplitByRanges, Ranges = null };
			await Assert.ThrowsAsync<ArgumentException>(() => _service.SplitAsync(stream, options));
		}

		[Fact]
		public async Task SplitAsync_ValidInputs_CallsProcessor()
		{
			var stream = new MemoryStream();
			await _service.SplitAsync(stream);
			Assert.True(_processor.SplitCalled);
		}

		[Fact]
		public async Task RotateAsync_InvalidDegrees_ThrowsArgumentException()
		{
			var stream = new MemoryStream();
			await Assert.ThrowsAsync<ArgumentException>(() => _service.RotateAsync(stream, 45));
		}

		[Fact]
		public async Task RotateAsync_ValidInputs_CallsProcessor()
		{
			var stream = new MemoryStream();
			await _service.RotateAsync(stream, 90);
			Assert.True(_processor.RotateCalled);
		}

		[Fact]
		public async Task CropAsync_NullOptions_ThrowsArgumentNullException()
		{
			var stream = new MemoryStream();
			await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CropAsync(stream, null!));
		}

		[Fact]
		public async Task CropAsync_NegativeCropBounds_ThrowsArgumentException()
		{
			var stream = new MemoryStream();
			var options = new CropOptions { Left = -10, Top = 0, Right = 0, Bottom = 0 };
			await Assert.ThrowsAsync<ArgumentException>(() => _service.CropAsync(stream, options));
		}

		[Fact]
		public async Task CropAsync_ValidInputs_CallsProcessor()
		{
			var stream = new MemoryStream();
			var options = new CropOptions { Left = 10, Top = 10, Right = 10, Bottom = 10 };
			await _service.CropAsync(stream, options);
			Assert.True(_processor.CropCalled);
		}

		[Fact]
		public async Task RemovePagesAsync_NullPageNumbers_ThrowsArgumentNullException()
		{
			var stream = new MemoryStream();
			await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RemovePagesAsync(stream, null!));
		}

		[Fact]
		public async Task RemovePagesAsync_EmptyPageNumbers_ThrowsArgumentException()
		{
			var stream = new MemoryStream();
			await Assert.ThrowsAsync<ArgumentException>(() => _service.RemovePagesAsync(stream, new List<int>()));
		}

		[Fact]
		public async Task RemovePagesAsync_ZeroPageNumber_ThrowsArgumentException()
		{
			var stream = new MemoryStream();
			await Assert.ThrowsAsync<ArgumentException>(() => _service.RemovePagesAsync(stream, new List<int> { 0 }));
		}

		[Fact]
		public async Task RemovePagesAsync_ValidInputs_CallsProcessor()
		{
			var stream = new MemoryStream();
			await _service.RemovePagesAsync(stream, new List<int> { 1, 2 });
			Assert.True(_processor.RemovePagesCalled);
		}

		[Fact]
		public async Task OcrAsync_NullOcrProvider_ThrowsPdfOcrException()
		{
			var serviceWithoutOcr = new PdfService(_processor, _renderer, _extractor, new MockLogger<PdfService>(), ocrProvider: null);
			var stream = new MemoryStream();
			await Assert.ThrowsAsync<PdfOcrException>(() => serviceWithoutOcr.OcrAsync(stream));
		}

		[Fact]
		public async Task OcrAsync_ValidInputs_CallsOcrProvider()
		{
			var stream = new MemoryStream();
			await _service.OcrAsync(stream);
			Assert.True(_ocrProvider.PerformOcrCalled);
		}

		[Fact]
		public async Task GetMetadataAsync_ValidInputs_CallsProcessor()
		{
			var stream = new MemoryStream();
			await _service.GetMetadataAsync(stream);
			Assert.True(_processor.GetMetadataCalled);
		}

		[Fact]
		public async Task SetMetadataAsync_ValidInputs_CallsProcessor()
		{
			var stream = new MemoryStream();
			var metadata = new PdfMetadata { Title = "New Title" };
			await _service.SetMetadataAsync(stream, metadata);
			Assert.True(_processor.SetMetadataCalled);
		}

		[Fact]
		public async Task ProtectAsync_ValidInputs_CallsProcessor()
		{
			var stream = new MemoryStream();
			var options = new ProtectionOptions { UserPassword = "user", OwnerPassword = "owner" };
			await _service.ProtectAsync(stream, options);
			Assert.True(_processor.ProtectCalled);
		}

		[Fact]
		public async Task UnprotectAsync_ValidInputs_CallsProcessor()
		{
			var stream = new MemoryStream();
			await _service.UnprotectAsync(stream, "password");
			Assert.True(_processor.UnprotectCalled);
		}

		[Fact]
		public async Task AddWatermarkAsync_ValidInputs_CallsProcessor()
		{
			var stream = new MemoryStream();
			var options = new WatermarkOptions { Text = "CONFIDENTIAL" };
			await _service.AddWatermarkAsync(stream, options);
			Assert.True(_processor.AddWatermarkCalled);
		}

		[Fact]
		public async Task ExtractPagesAsync_ValidInputs_CallsProcessor()
		{
			var stream = new MemoryStream();
			await _service.ExtractPagesAsync(stream, new[] { 1, 3 });
			Assert.True(_processor.ExtractPagesCalled);
		}
	}
}
