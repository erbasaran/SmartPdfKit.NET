using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SkiaSharp;
using SmartPdfKit.Exceptions;
using SmartPdfKit.Infrastructure.Pdfsharp;
using SmartPdfKit.Models;
using PdfMetadata = SmartPdfKit.Models.PdfMetadata;

namespace SmartPdfKit.Tests
{
	public class PdfsharpProcessorTests
	{
		private readonly PdfsharpProcessor _processor;

		public PdfsharpProcessorTests()
		{
			_processor = new PdfsharpProcessor();
		}

		private Stream CreateTestPdf(int pagesCount)
		{
			var document = new PdfDocument();
			for (int i = 0; i < pagesCount; i++)
			{
				var page = document.AddPage();
				using var gfx = XGraphics.FromPdfPage(page);
				var font = new XFont("Helvetica", 12, XFontStyleEx.Regular);
				gfx.DrawString($"Page {i + 1} Content", font, XBrushes.Black, 50, 50);
			}
			var ms = new MemoryStream();
			document.Save(ms);
			ms.Position = 0;
			return ms;
		}

		private Stream CreateTestImage()
		{
			var ms = new MemoryStream();
			using var bitmap = new SKBitmap(10, 10);
			using var image = SKImage.FromBitmap(bitmap);
			using var data = image.Encode(SKEncodedImageFormat.Png, 100);
			data.SaveTo(ms);
			ms.Position = 0;
			return ms;
		}

		[Fact]
		public async Task MergeAsync_MultiplePdfs_MergesPagesCorrectly()
		{
			using var pdf1 = CreateTestPdf(2);
			using var pdf2 = CreateTestPdf(3);

			using var merged = await _processor.MergeAsync(new[] { pdf1, pdf2 });

			using var doc = PdfReader.Open(merged, PdfDocumentOpenMode.Import);
			Assert.Equal(5, doc.PageCount);
		}

		[Fact]
		public async Task SplitAsync_SplitAll_CreatesIndividualPages()
		{
			using var source = CreateTestPdf(3);
			var options = new SplitOptions { Mode = SplitMode.SplitAll };

			var parts = (await _processor.SplitAsync(source, options)).ToList();

			Assert.Equal(3, parts.Count);
			foreach (var part in parts)
			{
				using (part)
				{
					using var doc = PdfReader.Open(part, PdfDocumentOpenMode.Import);
					Assert.Equal(1, doc.PageCount);
				}
			}
		}

		[Fact]
		public async Task SplitAsync_FixedSize_SplitsCorrectly()
		{
			using var source = CreateTestPdf(5);
			var options = new SplitOptions { Mode = SplitMode.SplitFixedSize, PageInterval = 2 };

			var parts = (await _processor.SplitAsync(source, options)).ToList();

			Assert.Equal(3, parts.Count); // 2 pages, 2 pages, 1 page

			using var doc1 = PdfReader.Open(parts[0], PdfDocumentOpenMode.Import);
			Assert.Equal(2, doc1.PageCount);

			using var doc2 = PdfReader.Open(parts[1], PdfDocumentOpenMode.Import);
			Assert.Equal(2, doc2.PageCount);

			using var doc3 = PdfReader.Open(parts[2], PdfDocumentOpenMode.Import);
			Assert.Equal(1, doc3.PageCount);

			foreach (var part in parts) part.Dispose();
		}

		[Fact]
		public async Task SplitAsync_Ranges_SplitsCorrectly()
		{
			using var source = CreateTestPdf(5);
			var options = new SplitOptions { Mode = SplitMode.SplitByRanges, Ranges = "1-2, 4-5" };

			var parts = (await _processor.SplitAsync(source, options)).ToList();

			Assert.Equal(2, parts.Count); // Doc 1 (pages 1,2), Doc 2 (pages 4,5)

			using var doc1 = PdfReader.Open(parts[0], PdfDocumentOpenMode.Import);
			Assert.Equal(2, doc1.PageCount);

			using var doc2 = PdfReader.Open(parts[1], PdfDocumentOpenMode.Import);
			Assert.Equal(2, doc2.PageCount);

			foreach (var part in parts) part.Dispose();
		}

		[Fact]
		public async Task RotateAsync_ValidInput_ChangesRotationSettings()
		{
			using var source = CreateTestPdf(1);

			using var rotated = await _processor.RotateAsync(source, 90);

			using var doc = PdfReader.Open(rotated, PdfDocumentOpenMode.Import);
			Assert.Equal(90, doc.Pages[0].Rotate);
		}

		[Fact]
		public async Task CropAsync_ValidInput_ChangesCropBox()
		{
			using var source = CreateTestPdf(1);
			var options = new CropOptions
			{
				Left = 10,
				Top = 10,
				Right = 10,
				Bottom = 10,
				UsePercentage = true
			};

			using var cropped = await _processor.CropAsync(source, options);

			using var doc = PdfReader.Open(cropped, PdfDocumentOpenMode.Import);
			var page = doc.Pages[0];
			Assert.NotEqual(page.MediaBox, page.CropBox);
		}

		[Fact]
		public async Task RemovePagesAsync_ValidInput_RemovesTargetPages()
		{
			using var source = CreateTestPdf(4);

			using var result = await _processor.RemovePagesAsync(source, new[] { 2, 4 });

			using var doc = PdfReader.Open(result, PdfDocumentOpenMode.Import);
			Assert.Equal(2, doc.PageCount);
		}

		[Fact]
		public async Task TextToPdfAsync_ValidInput_GeneratesPdf()
		{
			string sampleText = "Hello World\nLine 2\nLine 3 of text to render.";
			var options = new TextToPdfOptions();

			using var pdf = await _processor.TextToPdfAsync(sampleText, options);

			using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
			Assert.True(doc.PageCount >= 1);
		}

		[Fact]
		public async Task ImageToPdfAsync_ValidInput_GeneratesPdf()
		{
			using var img1 = CreateTestImage();
			using var img2 = CreateTestImage();
			var options = new ImageToPdfOptions { AutoPageSize = true };

			using var pdf = await _processor.ImageToPdfAsync(new[] { img1, img2 }, options);

			using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
			Assert.Equal(2, doc.PageCount);
		}

		[Fact]
		public async Task Metadata_SetAndGet_WorksCorrectly()
		{
			using var source = CreateTestPdf(1);
			var metadata = new PdfMetadata
			{
				Title = "My Test Document",
				Author = "Antigravity",
				Subject = "Unit Testing",
				Keywords = "pdf, test, smartpdfkit",
				Creator = "SmartPdfKit"
			};

			using var withMetadata = await _processor.SetMetadataAsync(source, metadata);
			var retrieved = await _processor.GetMetadataAsync(withMetadata);

			Assert.Equal(metadata.Title, retrieved.Title);
			Assert.Equal(metadata.Author, retrieved.Author);
			Assert.Equal(metadata.Subject, retrieved.Subject);
			Assert.Equal(metadata.Keywords, retrieved.Keywords);
			Assert.Equal(metadata.Creator, retrieved.Creator);
		}

		[Fact]
		public async Task Protection_ProtectAndUnprotect_WorksCorrectly()
		{
			using var source = CreateTestPdf(1);
			var options = new ProtectionOptions
			{
				UserPassword = "user123",
				OwnerPassword = "owner123",
				PermitPrint = false,
				PermitModify = false,
				PermitCopy = false
			};

			// Protect
			using var protectedStream = await _processor.ProtectAsync(source, options);

			// Accessing without password should throw a PdfProcessingException
			await Assert.ThrowsAsync<PdfProcessingException>(() => _processor.GetMetadataAsync(protectedStream));

			// Accessing with correct password should work
			protectedStream.Position = 0;
			var metadata = await _processor.GetMetadataAsync(protectedStream, "user123");
			Assert.NotNull(metadata);

			// Unprotect
			protectedStream.Position = 0;
			using var unprotectedStream = await _processor.UnprotectAsync(protectedStream, "owner123");

			// Accessing now without password should work
			var metadataAfterUnprotect = await _processor.GetMetadataAsync(unprotectedStream);
			Assert.NotNull(metadataAfterUnprotect);
		}

		[Fact]
		public async Task AddWatermarkAsync_ValidInput_AppliesWatermark()
		{
			using var source = CreateTestPdf(2);
			var options = new WatermarkOptions
			{
				Text = "CONFIDENTIAL",
				FontName = "Helvetica",
				FontSize = 36,
				Opacity = 0.4,
				Rotation = 30
			};

			using var watermarked = await _processor.AddWatermarkAsync(source, options);
			using var doc = PdfReader.Open(watermarked, PdfDocumentOpenMode.Import);
			Assert.Equal(2, doc.PageCount);
		}

		[Fact]
		public async Task ExtractPagesAsync_ValidInput_ExtractsCorrectPages()
		{
			using var source = CreateTestPdf(5);
			using var extracted = await _processor.ExtractPagesAsync(source, new[] { 2, 4, 5 });

			using var doc = PdfReader.Open(extracted, PdfDocumentOpenMode.Import);
			Assert.Equal(3, doc.PageCount);
		}

		private Stream CreateTestPdfWithImage()
		{
			var document = new PdfDocument();
			var page = document.AddPage();
			using (var gfx = XGraphics.FromPdfPage(page))
			{
				using var imgStream = CreateTestImage();
				using var xImage = XImage.FromStream(imgStream);
				gfx.DrawImage(xImage, 50, 50, 100, 100);
			}
			var ms = new MemoryStream();
			document.Save(ms);
			ms.Position = 0;
			return ms;
		}

		[Fact]
		public async Task CompressAsync_WithImage_RunsSuccessfully()
		{
			using var source = CreateTestPdfWithImage();
			var options = new CompressOptions
			{
				CompressContentStreams = true,
				ImageQuality = 50,
				RemoveMetadata = true
			};

			using var compressed = await _processor.CompressAsync(source, options);
			Assert.True(compressed.Length > 0);

			using var doc = PdfReader.Open(compressed, PdfDocumentOpenMode.Import);
			Assert.Equal(1, doc.PageCount);
		}

		[Fact]
		public async Task CompressAsync_WithPredictorAndIndexedImage_RunsSuccessfully()
		{
			// Build a PDF document containing a custom image dictionary with an indexed colorspace and a PNG predictor
			var document = new PdfDocument();
			var page = document.AddPage();

			// Create a custom image object in the PDF
			var imageDict = new PdfDictionary(document);
			imageDict.Elements.SetName("/Type", "/XObject");
			imageDict.Elements.SetName("/Subtype", "/Image");
			imageDict.Elements.SetInteger("/Width", 2);
			imageDict.Elements.SetInteger("/Height", 2);
			imageDict.Elements.SetInteger("/BitsPerComponent", 8);

			// Set /ColorSpace as an indexed colorspace: [/Indexed /DeviceRGB 1 <000000FFFFFF>]
			var csArray = new PdfArray(document);
			csArray.Elements.Add(new PdfName("/Indexed"));
			csArray.Elements.Add(new PdfName("/DeviceRGB"));
			csArray.Elements.Add(new PdfInteger(1));
			// Lookup table containing 2 RGB colors: black (0,0,0) and white (255,255,255)
			csArray.Elements.Add(new PdfString("\x00\x00\x00\xff\xff\xff", PdfStringEncoding.RawEncoding));
			imageDict.Elements["/ColorSpace"] = csArray;

			// Set /DecodeParms with a Predictor (e.g. 15 PNG optimum predictor)
			var decodeParms = new PdfDictionary(document);
			decodeParms.Elements.SetInteger("/Predictor", 15);
			imageDict.Elements["/DecodeParms"] = decodeParms;

			// Raw data representing 2x2 indexed pixels with PNG predictor prefix bytes:
			// Stride: 1 prefix byte + 2 bytes data = 3 bytes per row. Total 2 rows = 6 bytes.
			// Row 1: predictor 0 (None), pixels [0, 1]
			// Row 2: predictor 0 (None), pixels [1, 0]
			byte[] rawBytes = new byte[] { 0, 0, 1, 0, 1, 0 };

			// Assign stream
			imageDict.CreateStream(rawBytes);

			// Add image to page resources
			var resources = page.Elements.GetDictionary("/Resources");
			if (resources == null)
			{
				resources = new PdfDictionary(document);
				page.Elements["/Resources"] = resources;
			}
			var xObjects = new PdfDictionary(document);
			resources.Elements["/XObject"] = xObjects;

			// Reference the image
			document.Internals.AddObject(imageDict);
			xObjects.Elements["/Img1"] = imageDict.Reference;

			var ms = new MemoryStream();
			document.Save(ms);
			ms.Position = 0;

			// Now, compress the PDF using our processor
			var options = new CompressOptions
			{
				CompressContentStreams = true,
				ImageQuality = 50,
				MaxImageDimension = 100,
				RemoveMetadata = true
			};

			using var compressed = await _processor.CompressAsync(ms, options);
			Assert.True(compressed.Length > 0);

			// Read the compressed PDF to verify it is valid
			using var doc = PdfReader.Open(compressed, PdfDocumentOpenMode.Import);
			Assert.Equal(1, doc.PageCount);
		}
	}
}
