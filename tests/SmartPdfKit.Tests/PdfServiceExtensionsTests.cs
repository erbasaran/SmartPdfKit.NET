using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using SmartPdfKit.Extensions;
using SmartPdfKit.Models;
using SmartPdfKit.Services;

namespace SmartPdfKit.Tests
{
	public class PdfServiceExtensionsTests
	{
		private readonly MockPdfProcessor _processor;
		private readonly MockPdfRenderer _renderer;
		private readonly MockPdfTextExtractor _extractor;
		private readonly MockOcrProvider _ocrProvider;
		private readonly PdfService _service;

		public PdfServiceExtensionsTests()
		{
			_processor = new MockPdfProcessor();
			_renderer = new MockPdfRenderer();
			_extractor = new MockPdfTextExtractor();
			_ocrProvider = new MockOcrProvider();
			_service = new PdfService(_processor, _renderer, _extractor, new MockLogger<PdfService>(), _ocrProvider);
		}

		[Fact]
		public async Task MergeAsync_ByteArrays_CallsProcessor()
		{
			var pdfList = new List<byte[]> { Array.Empty<byte>() };
			var result = await _service.MergeAsync(pdfList);

			Assert.True(_processor.MergeCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task SplitAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var result = await _service.SplitAsync(pdfBytes);

			Assert.True(_processor.SplitCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task CompressAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var result = await _service.CompressAsync(pdfBytes);

			Assert.True(_processor.CompressCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task RotateAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var result = await _service.RotateAsync(pdfBytes, 90);

			Assert.True(_processor.RotateCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task CropAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var options = new CropOptions { Left = 10, Top = 10, Right = 10, Bottom = 10 };
			var result = await _service.CropAsync(pdfBytes, options);

			Assert.True(_processor.CropCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task RemovePagesAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var pages = new List<int> { 1 };
			var result = await _service.RemovePagesAsync(pdfBytes, pages);

			Assert.True(_processor.RemovePagesCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task OcrAsync_ByteArray_CallsOcrProvider()
		{
			var pdfBytes = Array.Empty<byte>();
			var result = await _service.OcrAsync(pdfBytes);

			Assert.True(_ocrProvider.PerformOcrCalled);
			Assert.NotNull(result);
			Assert.Equal("OCR Mock Text", result.Text);
		}

		[Fact]
		public async Task ConvertToImagesAsync_ByteArray_CallsRenderer()
		{
			var pdfBytes = Array.Empty<byte>();
			var result = await _service.ConvertToImagesAsync(pdfBytes);

			Assert.True(_renderer.RenderToImagesCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task ConvertToPdfAsync_ByteArrays_CallsProcessor()
		{
			var images = new List<byte[]> { Array.Empty<byte>() };
			var result = await _service.ConvertToPdfAsync(images);

			Assert.True(_processor.ImageToPdfCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task ConvertToTextAsync_ByteArray_CallsExtractor()
		{
			var pdfBytes = Array.Empty<byte>();
			var result = await _service.ConvertToTextAsync(pdfBytes);

			Assert.True(_extractor.ExtractTextCalled);
			Assert.Equal("Mock Text Content", result);
		}

		[Fact]
		public async Task ConvertTextToPdfAsync_Text_CallsProcessor()
		{
			var result = await _service.ConvertTextToPdfAsync("Sample Text");

			Assert.True(_processor.TextToPdfCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task GetMetadataAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var result = await _service.GetMetadataAsync(pdfBytes);

			Assert.True(_processor.GetMetadataCalled);
			Assert.Equal("Mock Title", result.Title);
		}

		[Fact]
		public async Task SetMetadataAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var metadata = new PdfMetadata { Title = "Updated Title" };
			var result = await _service.SetMetadataAsync(pdfBytes, metadata);

			Assert.True(_processor.SetMetadataCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task ProtectAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var options = new ProtectionOptions { UserPassword = "user", OwnerPassword = "owner" };
			var result = await _service.ProtectAsync(pdfBytes, options);

			Assert.True(_processor.ProtectCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task UnprotectAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var result = await _service.UnprotectAsync(pdfBytes, "password");

			Assert.True(_processor.UnprotectCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task AddWatermarkAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var options = new WatermarkOptions { Text = "CONFIDENTIAL" };
			var result = await _service.AddWatermarkAsync(pdfBytes, options);

			Assert.True(_processor.AddWatermarkCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task ExtractPagesAsync_ByteArray_CallsProcessor()
		{
			var pdfBytes = Array.Empty<byte>();
			var pages = new List<int> { 1, 2 };
			var result = await _service.ExtractPagesAsync(pdfBytes, pages);

			Assert.True(_processor.ExtractPagesCalled);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task OcrResult_GetSearchablePdfBytes_ReturnsExpectedBytes()
		{
			var pdfBytes = Array.Empty<byte>();
			var options = new OcrOptions { Mode = OcrMode.SearchablePdf };
			using var ocrResult = await _service.OcrAsync(pdfBytes, options);

			var searchableBytes = ocrResult.GetSearchablePdfBytes();
			Assert.NotNull(searchableBytes);
		}
	}
}
