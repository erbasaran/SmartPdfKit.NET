using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartPdfKit.Interfaces;
using SmartPdfKit.Models;

namespace SmartPdfKit.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="IPdfService"/> providing file-path based overloads.
	/// </summary>
	public static class PdfServiceExtensions
	{
		/// <summary>
		/// Merges multiple PDF files into a single output PDF file.
		/// </summary>
		public static async Task MergeAsync(
			this IPdfService service,
			IEnumerable<string> filePaths,
			string destinationPath,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (filePaths == null) throw new ArgumentNullException(nameof(filePaths));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

			var files = filePaths.ToList();
			var openStreams = new List<FileStream>();
			try
			{
				foreach (var path in files)
				{
					openStreams.Add(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
				}

				using var mergedStream = await service.MergeAsync(openStreams, cancellationToken).ConfigureAwait(false);
				using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
				await mergedStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				foreach (var stream in openStreams)
				{
					stream.Dispose();
				}
			}
		}

		/// <summary>
		/// Splits a PDF file into multiple PDF files saved to a directory.
		/// </summary>
		public static async Task SplitAsync(
			this IPdfService service,
			string filePath,
			string outputDirectory,
			string fileNamePattern = "split_page_{0}.pdf",
			SplitOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Source file path cannot be empty.", nameof(filePath));
			if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("Output directory cannot be empty.", nameof(outputDirectory));
			if (string.IsNullOrWhiteSpace(fileNamePattern)) throw new ArgumentException("Filename pattern cannot be empty.", nameof(fileNamePattern));

			Directory.CreateDirectory(outputDirectory);

			using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var splitStreams = (await service.SplitAsync(sourceStream, options, cancellationToken).ConfigureAwait(false)).ToList();
			try
			{
				for (int i = 0; i < splitStreams.Count; i++)
				{
					string outPath = Path.Combine(outputDirectory, string.Format(fileNamePattern, i + 1));
					using var destStream = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None);
					await splitStreams[i].CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
				}
			}
			finally
			{
				foreach (var stream in splitStreams)
				{
					stream.Dispose();
				}
			}
		}

		/// <summary>
		/// Compresses a PDF file and saves it to a destination.
		/// </summary>
		public static async Task CompressAsync(
			this IPdfService service,
			string sourcePath,
			string destinationPath,
			CompressOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Source path cannot be empty.", nameof(sourcePath));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

			using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var compressedStream = await service.CompressAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
			using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await compressedStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Rotates pages of a PDF file and saves to a destination.
		/// </summary>
		public static async Task RotateAsync(
			this IPdfService service,
			string sourcePath,
			string destinationPath,
			int rotationDegrees,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Source path cannot be empty.", nameof(sourcePath));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

			using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var rotatedStream = await service.RotateAsync(sourceStream, rotationDegrees, cancellationToken).ConfigureAwait(false);
			using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await rotatedStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Crops pages of a PDF file and saves to a destination.
		/// </summary>
		public static async Task CropAsync(
			this IPdfService service,
			string sourcePath,
			string destinationPath,
			CropOptions options,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Source path cannot be empty.", nameof(sourcePath));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));
			if (options == null) throw new ArgumentNullException(nameof(options));

			using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var croppedStream = await service.CropAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
			using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await croppedStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Removes pages from a PDF file and saves to a destination.
		/// </summary>
		public static async Task RemovePagesAsync(
			this IPdfService service,
			string sourcePath,
			string destinationPath,
			IEnumerable<int> pageNumbers,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Source path cannot be empty.", nameof(sourcePath));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));
			if (pageNumbers == null) throw new ArgumentNullException(nameof(pageNumbers));

			using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var remainingStream = await service.RemovePagesAsync(sourceStream, pageNumbers, cancellationToken).ConfigureAwait(false);
			using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await remainingStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Performs OCR on a PDF file.
		/// </summary>
		public static async Task<OcrResult> OcrAsync(
			this IPdfService service,
			string filePath,
			OcrOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty.", nameof(filePath));

			using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			return await service.OcrAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Converts PDF pages to image files in a directory.
		/// </summary>
		public static async Task ConvertToImagesAsync(
			this IPdfService service,
			string pdfPath,
			string outputDirectory,
			string fileNamePattern = "page_{0}.png",
			ImageConversionOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(pdfPath)) throw new ArgumentException("PDF path cannot be empty.", nameof(pdfPath));
			if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("Output directory cannot be empty.", nameof(outputDirectory));
			if (string.IsNullOrWhiteSpace(fileNamePattern)) throw new ArgumentException("Filename pattern cannot be empty.", nameof(fileNamePattern));

			Directory.CreateDirectory(outputDirectory);

			using var sourceStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var imageStreams = (await service.ConvertToImagesAsync(sourceStream, options, cancellationToken).ConfigureAwait(false)).ToList();
			try
			{
				for (int i = 0; i < imageStreams.Count; i++)
				{
					string ext = (options?.Format ?? ImageFormat.Png) == ImageFormat.Png ? "png" : "jpg";
					string actualPattern = fileNamePattern.Contains("{0}") ? fileNamePattern : fileNamePattern + "_{0}." + ext;
					string outPath = Path.Combine(outputDirectory, string.Format(actualPattern, i + 1));

					using var destStream = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None);
					await imageStreams[i].CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
				}
			}
			finally
			{
				foreach (var stream in imageStreams)
				{
					stream.Dispose();
				}
			}
		}

		/// <summary>
		/// Converts multiple image files into a single PDF file.
		/// </summary>
		public static async Task ConvertToPdfAsync(
			this IPdfService service,
			IEnumerable<string> imagePaths,
			string destinationPath,
			ImageToPdfOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (imagePaths == null) throw new ArgumentNullException(nameof(imagePaths));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

			var files = imagePaths.ToList();
			var openStreams = new List<FileStream>();
			try
			{
				foreach (var path in files)
				{
					openStreams.Add(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
				}

				using var pdfStream = await service.ConvertToPdfAsync(openStreams, options, cancellationToken).ConfigureAwait(false);
				using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
				await pdfStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				foreach (var stream in openStreams)
				{
					stream.Dispose();
				}
			}
		}

		/// <summary>
		/// Extracts text from a PDF file.
		/// </summary>
		public static async Task<string> ConvertToTextAsync(
			this IPdfService service,
			string pdfPath,
			TextExtractionOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(pdfPath)) throw new ArgumentException("PDF path cannot be empty.", nameof(pdfPath));

			using var sourceStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			return await service.ConvertToTextAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Converts raw text to a PDF file.
		/// </summary>
		public static async Task ConvertTextToPdfAsync(
			this IPdfService service,
			string text,
			string destinationPath,
			TextToPdfOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (text == null) throw new ArgumentNullException(nameof(text));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

			using var pdfStream = await service.ConvertTextToPdfAsync(text, options, cancellationToken).ConfigureAwait(false);
			using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await pdfStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the document metadata of a PDF file.
		/// </summary>
		public static async Task<PdfMetadata> GetMetadataAsync(
			this IPdfService service,
			string filePath,
			string? password = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty.", nameof(filePath));

			using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			return await service.GetMetadataAsync(sourceStream, password, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Sets the document metadata of a PDF file.
		/// </summary>
		public static async Task SetMetadataAsync(
			this IPdfService service,
			string sourcePath,
			string destinationPath,
			PdfMetadata metadata,
			string? password = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Source path cannot be empty.", nameof(sourcePath));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));
			if (metadata == null) throw new ArgumentNullException(nameof(metadata));

			using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var outputStream = await service.SetMetadataAsync(sourceStream, metadata, password, cancellationToken).ConfigureAwait(false);
			using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await outputStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Encrypts and protects a PDF file.
		/// </summary>
		public static async Task ProtectAsync(
			this IPdfService service,
			string sourcePath,
			string destinationPath,
			ProtectionOptions options,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Source path cannot be empty.", nameof(sourcePath));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));
			if (options == null) throw new ArgumentNullException(nameof(options));

			using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var outputStream = await service.ProtectAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
			using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await outputStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Decrypts a protected PDF file.
		/// </summary>
		public static async Task UnprotectAsync(
			this IPdfService service,
			string sourcePath,
			string destinationPath,
			string password,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Source path cannot be empty.", nameof(sourcePath));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));
			if (password == null) throw new ArgumentNullException(nameof(password));

			using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var outputStream = await service.UnprotectAsync(sourceStream, password, cancellationToken).ConfigureAwait(false);
			using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await outputStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Adds a text watermark to a PDF file.
		/// </summary>
		public static async Task AddWatermarkAsync(
			this IPdfService service,
			string sourcePath,
			string destinationPath,
			WatermarkOptions options,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Source path cannot be empty.", nameof(sourcePath));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));
			if (options == null) throw new ArgumentNullException(nameof(options));

			using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var outputStream = await service.AddWatermarkAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
			using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await outputStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Extracts specific page numbers from a PDF file.
		/// </summary>
		public static async Task ExtractPagesAsync(
			this IPdfService service,
			string sourcePath,
			string destinationPath,
			IEnumerable<int> pageNumbers,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Source path cannot be empty.", nameof(sourcePath));
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));
			if (pageNumbers == null) throw new ArgumentNullException(nameof(pageNumbers));

			using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var outputStream = await service.ExtractPagesAsync(sourceStream, pageNumbers, cancellationToken).ConfigureAwait(false);
			using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await outputStream.CopyToAsync(destStream, 81920, cancellationToken).ConfigureAwait(false);
		}
	}
}
