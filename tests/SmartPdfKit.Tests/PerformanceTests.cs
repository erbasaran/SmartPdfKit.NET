using SmartPdfKit.Infrastructure.Pdfsharp;

namespace SmartPdfKit.Tests
{
	public class PerformanceTests
	{
		private readonly PdfsharpProcessor _processor = new();

		[Fact]
		public async Task MergePerformance_LowMemoryFootprint()
		{
			long startMemory = GC.GetTotalMemory(true);

			var streams = new List<Stream>();
			for (int i = 0; i < 5; i++)
			{
				streams.Add(CreateTestPdf(10)); // Create five 10-page documents
			}

			using var merged = await _processor.MergeAsync(streams);
			Assert.True(merged.Length > 0);

			foreach (var stream in streams)
			{
				stream.Dispose();
			}

			long endMemory = GC.GetTotalMemory(true);
			long diff = Math.Abs(endMemory - startMemory);

			// Memory difference should be reasonable (e.g. less than 50MB)
			Assert.True(diff < 50 * 1024 * 1024, $"Memory usage was too high: {diff / 1024.0 / 1024.0} MB");
		}

		private Stream CreateTestPdf(int pagesCount)
		{
			var document = new PdfSharp.Pdf.PdfDocument();
			for (int i = 0; i < pagesCount; i++)
			{
				var page = document.AddPage();
				using var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
				var font = new PdfSharp.Drawing.XFont("Helvetica", 10, PdfSharp.Drawing.XFontStyleEx.Regular);
				gfx.DrawString($"Page {i + 1} Content for performance test.", font, PdfSharp.Drawing.XBrushes.Black, 50, 50);
			}
			var ms = new MemoryStream();
			document.Save(ms);
			ms.Position = 0;
			return ms;
		}
	}
}
