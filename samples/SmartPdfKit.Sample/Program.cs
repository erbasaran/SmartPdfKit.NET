using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartPdfKit.Builder;
using SmartPdfKit.Exceptions;
using SmartPdfKit.Extensions;
using SmartPdfKit.Interfaces;
using SmartPdfKit.Models;

namespace SmartPdfKit.Sample
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.Title = "SmartPdfKit Demonstration";

			// 1. Setup DI Container
			var serviceProvider = new ServiceCollection()
				.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information))
				.AddSmartPdfKit()
				.BuildServiceProvider();

			var pdfService = serviceProvider.GetRequiredService<IPdfService>();

			PrintHeader();

			try
			{
				// Create directories for inputs and outputs
				string outputDir = Path.Combine(AppContext.BaseDirectory, "DemoOutputs");
				Directory.CreateDirectory(outputDir);

				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine($"[INFO] Output files will be generated in: {outputDir}\n");
				Console.ResetColor();

				// Define file paths
				string textPdfPath = Path.Combine(outputDir, "1_text_to_pdf.pdf");
				string rotatedPdfPath = Path.Combine(outputDir, "2_rotated.pdf");
				string croppedPdfPath = Path.Combine(outputDir, "3_cropped.pdf");
				string pagesRemovedPdfPath = Path.Combine(outputDir, "4_pages_removed.pdf");
				string compressedPdfPath = Path.Combine(outputDir, "5_compressed.pdf");
				string mergedPdfPath = Path.Combine(outputDir, "6_merged.pdf");
				string imagesDir = Path.Combine(outputDir, "7_images");
				string imagesPdfPath = Path.Combine(outputDir, "8_images_to_pdf.pdf");
				string splitDir = Path.Combine(outputDir, "9_splits");
				string metadataPdfPath = Path.Combine(outputDir, "10_metadata.pdf");
				string protectedPdfPath = Path.Combine(outputDir, "11_protected.pdf");
				string unprotectedPdfPath = Path.Combine(outputDir, "12_unprotected.pdf");
				string watermarkedPdfPath = Path.Combine(outputDir, "13_watermarked.pdf");
				string extractedPdfPath = Path.Combine(outputDir, "14_extracted.pdf");

				// ==========================================
				// 1. Convert Text to PDF
				// ==========================================
				PrintStep("1. Converting Text to PDF");
				string textContent = "SmartPdfKit Demo Document\n" +
									 "=========================\n" +
									 "This is page 1 content. SmartPdfKit is an enterprise-grade C# PDF toolkit.\n" +
									 "\f\n" +
									 "This is page 2 content. We support merging, splitting, cropping, and rotating.\n" +
									 "\f\n" +
									 "This is page 3 content. Text extraction and OCR are fully supported.";

				await pdfService.ConvertTextToPdfAsync(textContent, textPdfPath, new TextToPdfOptions
				{
					FontName = "Helvetica",
					FontSize = 12,
					Margin = 50
				});
				PrintSuccess($"Text successfully converted. File saved: {Path.GetFileName(textPdfPath)}");

				// ==========================================
				// 2. Rotate PDF (90 Degrees)
				// ==========================================
				PrintStep("2. Rotating PDF (90 Degrees)");
				await pdfService.RotateAsync(textPdfPath, rotatedPdfPath, 90);
				PrintSuccess($"PDF successfully rotated. File saved: {Path.GetFileName(rotatedPdfPath)}");

				// ==========================================
				// 3. Crop PDF Pages (10% margins)
				// ==========================================
				PrintStep("3. Cropping PDF (10% margin on all sides)");
				var cropOptions = new CropOptions
				{
					Left = 10,
					Top = 10,
					Right = 10,
					Bottom = 10,
					UsePercentage = true
				};
				await pdfService.CropAsync(textPdfPath, croppedPdfPath, cropOptions);
				PrintSuccess($"PDF successfully cropped. File saved: {Path.GetFileName(croppedPdfPath)}");

				// ==========================================
				// 4. Remove Pages (Remove page 2)
				// ==========================================
				PrintStep("4. Removing Pages (Removing page 2)");
				await pdfService.RemovePagesAsync(textPdfPath, pagesRemovedPdfPath, new[] { 2 });
				PrintSuccess($"Pages successfully removed. File saved: {Path.GetFileName(pagesRemovedPdfPath)}");

				// ==========================================
				// 5. Compress PDF Content Streams
				// ==========================================
				PrintStep("5. Compressing PDF Streams");
				var compressOptions = new CompressOptions
				{
					CompressContentStreams = true,
					ImageQuality = 60,             // 60 is optimal for high visual quality & excellent compression size reduction
					MaxImageDimension = 1200,      // Keep details crisp while resizing huge images
					RemoveMetadata = true
				};
				await pdfService.CompressAsync(textPdfPath, compressedPdfPath, compressOptions);
				PrintSuccess($"PDF successfully compressed. File saved: {Path.GetFileName(compressedPdfPath)}");

				// ==========================================
				// 6. Merge Multiple PDFs
				// ==========================================
				PrintStep("6. Merging Multiple PDFs");
				var filesToMerge = new[] { croppedPdfPath, pagesRemovedPdfPath };
				await pdfService.MergeAsync(filesToMerge, mergedPdfPath);
				PrintSuccess($"PDFs successfully merged. File saved: {Path.GetFileName(mergedPdfPath)}");

				// ==========================================
				// 7. Convert PDF Pages to Images
				// ==========================================
				PrintStep("7. Converting PDF Pages to PNG Images");
				var imgOptions = new ImageConversionOptions
				{
					Format = ImageFormat.Png,
					Dpi = 150
				};
				await pdfService.ConvertToImagesAsync(textPdfPath, imagesDir, "page_{0}.png", imgOptions);
				PrintSuccess($"PDF pages converted to images in: {Path.GetFileName(imagesDir)}");

				// ==========================================
				// 8. Convert Images back to PDF
				// ==========================================
				PrintStep("8. Converting Images back to a PDF");
				var imageFiles = Directory.GetFiles(imagesDir, "*.png").OrderBy(f => f).ToList();
				await pdfService.ConvertToPdfAsync(imageFiles, imagesPdfPath, new ImageToPdfOptions
				{
					AutoPageSize = true
				});
				PrintSuccess($"Images converted back to PDF. File saved: {Path.GetFileName(imagesPdfPath)}");

				// ==========================================
				// 9. Split PDF (by 1-page intervals)
				// ==========================================
				PrintStep("9. Splitting PDF into single-page documents");
				var splitOptions = new SplitOptions
				{
					Mode = SplitMode.SplitAll
				};
				await pdfService.SplitAsync(textPdfPath, splitDir, "page_{0}.pdf", splitOptions);
				PrintSuccess($"PDF split into individual pages in: {Path.GetFileName(splitDir)}");

				// ==========================================
				// 10. Extract Text from PDF
				// ==========================================
				PrintStep("10. Extracting Text from PDF");
				string extractedText = await pdfService.ConvertToTextAsync(textPdfPath);
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine("----- Extracted Text Output -----");
				Console.Write(extractedText);
				Console.WriteLine("---------------------------------");
				Console.ResetColor();
				PrintSuccess("Text successfully extracted.");

				// ==========================================
				// 11. Fluent Builder API (SmartPdfEditor)
				// ==========================================
				PrintStep("11. Demonstrating Fluent Builder API (SmartPdfEditor)");
				string fluentPdfPath = Path.Combine(outputDir, "10_fluent_edited.pdf");

				using (var editor = SmartPdfEditor.Open(textPdfPath, pdfService))
				{
					await editor
						.Rotate(180)
						.Crop(new CropOptions { Left = 5, Top = 5, Right = 5, Bottom = 5, UsePercentage = true })
						.Compress(new CompressOptions { CompressContentStreams = true })
						.SaveAsync(fluentPdfPath);
				}
				PrintSuccess($"Fluent pipeline operations completed. File saved: {Path.GetFileName(fluentPdfPath)}");

				// ==========================================
				// 11.1 PDF Metadata Editing
				// ==========================================
				PrintStep("11.1 Reading and Editing PDF Metadata");
				var initialMeta = await pdfService.GetMetadataAsync(textPdfPath);
				Console.WriteLine($"[INFO] Original Title: '{initialMeta.Title}', Author: '{initialMeta.Author}'");

				var updatedMeta = new PdfMetadata
				{
					Title = "SmartPdfKit Advanced Demo Guide",
					Author = "Antigravity",
					Subject = "Metadata, Encryption & Watermarks",
					Keywords = "pdf, csharp, standard, smartpdfkit",
					Creator = "SmartPdfKit Engine",
					CreationDate = DateTime.Now
				};
				await pdfService.SetMetadataAsync(textPdfPath, metadataPdfPath, updatedMeta);

				var verifiedMeta = await pdfService.GetMetadataAsync(metadataPdfPath);
				Console.WriteLine($"[INFO] New Title: '{verifiedMeta.Title}', Author: '{verifiedMeta.Author}'");
				PrintSuccess($"PDF metadata successfully updated. Saved: {Path.GetFileName(metadataPdfPath)}");

				// ==========================================
				// 11.2 PDF Protection & Unprotection
				// ==========================================
				PrintStep("11.2 Protecting and Decrypting PDF Document");
				var protectionOptions = new ProtectionOptions
				{
					UserPassword = "user123",
					OwnerPassword = "owner123",
					PermitPrint = true,
					PermitModify = false,
					PermitCopy = false
				};
				await pdfService.ProtectAsync(textPdfPath, protectedPdfPath, protectionOptions);
				Console.WriteLine("[INFO] PDF protected with passwords user123/owner123.");

				// Verify protection by reading metadata with password
				var protectedMeta = await pdfService.GetMetadataAsync(protectedPdfPath, "user123");
				Console.WriteLine($"[INFO] Verified protected PDF. Title: '{protectedMeta.Title}'");

				// Decrypt / unprotect
				await pdfService.UnprotectAsync(protectedPdfPath, unprotectedPdfPath, "owner123");
				PrintSuccess($"PDF successfully decrypted/unprotected. Saved: {Path.GetFileName(unprotectedPdfPath)}");

				// ==========================================
				// 11.3 Text Watermarking
				// ==========================================
				PrintStep("11.3 Adding Text Watermark to PDF");
				var watermarkOptions = new WatermarkOptions
				{
					Text = "INTERNAL ONLY",
					FontName = "Helvetica",
					FontSize = 40,
					Opacity = 0.25,
					Rotation = 45
				};
				await pdfService.AddWatermarkAsync(textPdfPath, watermarkedPdfPath, watermarkOptions);
				PrintSuccess($"Text watermark successfully applied. Saved: {Path.GetFileName(watermarkedPdfPath)}");

				// ==========================================
				// 11.4 Page Extraction
				// ==========================================
				PrintStep("11.4 Extracting Pages from PDF");
				// Extract pages 1 and 3 from our 3-page original PDF
				await pdfService.ExtractPagesAsync(textPdfPath, extractedPdfPath, new[] { 1, 3 });
				PrintSuccess($"Pages 1 and 3 successfully extracted. Saved: {Path.GetFileName(extractedPdfPath)}");

				// ==========================================
				// 12. Run OCR (Tesseract)
				// ==========================================
				PrintStep("12. Running OCR (Tesseract Engine)");
				Console.WriteLine("[INFO] Tesseract OCR requires language files (.traineddata).");
				Console.WriteLine("[INFO] Please download the relevant files (e.g., eng.traineddata) from:");
				Console.WriteLine("[INFO] https://github.com/tesseract-ocr/tessdata_best");
				Console.WriteLine("[INFO] Place them in a folder and specify the path in OcrOptions.TessDataPath.");
				try
				{
					// Using the pages generated as images, run OCR on the first page
					if (imageFiles.Count > 0)
					{
						var ocrOptions = new OcrOptions
						{
							Language = "eng",
							Mode = OcrMode.TextOnly
							// To run OCR, set the path to your downloaded tessdata files:
							// TessDataPath = @"C:\Path\To\tessdata"
						};

						using var imageStream = new FileStream(imageFiles[0], FileMode.Open, FileAccess.Read);
						// We run OCR by converting the image to PDF, and then OCRing it
						using var singlePagePdfStream = await pdfService.ConvertToPdfAsync(new[] { imageStream }, new ImageToPdfOptions());

						var ocrResult = await pdfService.OcrAsync(singlePagePdfStream, ocrOptions);

						Console.WriteLine($"OCR Mean Confidence: {ocrResult.Confidence:P2}");
						Console.ForegroundColor = ConsoleColor.DarkYellow;
						Console.WriteLine("----- OCR Extracted Text -----");
						Console.Write(ocrResult.Text);
						Console.WriteLine("------------------------------");
						Console.ResetColor();
						PrintSuccess("OCR completed successfully.");
					}
				}
				catch (PdfOcrException ocrEx)
				{
					PrintWarning($"OCR skipped/failed as expected if Tesseract is not installed: {ocrEx.Message}");
				}

				PrintSuccess("\nAll demo operations completed successfully!");
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"\n[ERROR] An unexpected error occurred: {ex.Message}");
				Console.WriteLine(ex.StackTrace);
				Console.ResetColor();
			}

			if (!Console.IsInputRedirected)
			{
				Console.WriteLine("\nPress any key to exit...");
				Console.ReadKey();
			}
			else
			{
				Console.WriteLine("\nRunning in non-interactive mode. Exiting.");
			}
		}

		private static void PrintHeader()
		{
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine("====================================================");
			Console.WriteLine("     SmartPdfKit Production Library Demonstration   ");
			Console.WriteLine("====================================================");
			Console.ResetColor();
		}

		private static void PrintStep(string stepTitle)
		{
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine($"\n>>> {stepTitle}...");
			Console.ResetColor();
		}

		private static void PrintSuccess(string message)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[SUCCESS] {message}");
			Console.ResetColor();
		}

		private static void PrintWarning(string message)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"[WARNING] {message}");
			Console.ResetColor();
		}
	}
}
