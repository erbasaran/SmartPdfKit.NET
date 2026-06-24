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

		/// <summary>
		/// Helper to convert a Stream to byte[] asynchronously and dispose of the stream.
		/// </summary>
		private static async Task<byte[]> StreamToBytesAndDisposeAsync(Stream stream, CancellationToken cancellationToken)
		{
			if (stream == null) return Array.Empty<byte>();
			try
			{
				if (stream is MemoryStream ms)
				{
					return ms.ToArray();
				}
				using var msTemp = new MemoryStream();
				if (stream.CanSeek && stream.Position != 0)
				{
					stream.Position = 0;
				}
				await stream.CopyToAsync(msTemp, 81920, cancellationToken).ConfigureAwait(false);
				return msTemp.ToArray();
			}
			finally
			{
				stream.Dispose();
			}
		}

		/// <summary>
		/// Merges multiple PDF byte arrays into a single output PDF byte array.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytesList">The collection of PDF byte arrays to merge.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A byte array containing the merged PDF.</returns>
		public static async Task<byte[]> MergeAsync(
			this IPdfService service,
			IEnumerable<byte[]> pdfBytesList,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytesList == null) throw new ArgumentNullException(nameof(pdfBytesList));

			var streams = new List<MemoryStream>();
			try
			{
				foreach (var bytes in pdfBytesList)
				{
					if (bytes == null) throw new ArgumentException("PDF byte array collection contains a null reference.", nameof(pdfBytesList));
					streams.Add(new MemoryStream(bytes));
				}

				var mergedStream = await service.MergeAsync(streams, cancellationToken).ConfigureAwait(false);
				return await StreamToBytesAndDisposeAsync(mergedStream, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				foreach (var stream in streams)
				{
					stream.Dispose();
				}
			}
		}

		/// <summary>
		/// Splits a single PDF byte array into multiple PDF byte arrays based on split options.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array to split.</param>
		/// <param name="options">Options specifying how to split the PDF.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A collection of byte arrays, each representing a split PDF file.</returns>
		public static async Task<IEnumerable<byte[]>> SplitAsync(
			this IPdfService service,
			byte[] pdfBytes,
			SplitOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));

			using var sourceStream = new MemoryStream(pdfBytes);
			var splitStreams = await service.SplitAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
			var streamList = splitStreams.ToList();
			var results = new List<byte[]>();
			try
			{
				foreach (var stream in streamList)
				{
					results.Add(await StreamToBytesAndDisposeAsync(stream, cancellationToken).ConfigureAwait(false));
				}
				return results;
			}
			catch
			{
				foreach (var stream in streamList)
				{
					stream?.Dispose();
				}
				throw;
			}
		}

		/// <summary>
		/// Compresses a PDF byte array to reduce its file size.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array to compress.</param>
		/// <param name="options">Compression options.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A compressed PDF byte array.</returns>
		public static async Task<byte[]> CompressAsync(
			this IPdfService service,
			byte[] pdfBytes,
			CompressOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));

			using var sourceStream = new MemoryStream(pdfBytes);
			var compressedStream = await service.CompressAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
			return await StreamToBytesAndDisposeAsync(compressedStream, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Rotates the pages of a PDF document by a specified degree.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array to rotate.</param>
		/// <param name="rotationDegrees">The rotation angle in degrees (must be a multiple of 90, e.g. 90, 180, 270).</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A rotated PDF byte array.</returns>
		public static async Task<byte[]> RotateAsync(
			this IPdfService service,
			byte[] pdfBytes,
			int rotationDegrees,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));

			using var sourceStream = new MemoryStream(pdfBytes);
			var rotatedStream = await service.RotateAsync(sourceStream, rotationDegrees, cancellationToken).ConfigureAwait(false);
			return await StreamToBytesAndDisposeAsync(rotatedStream, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Crops the pages of a PDF document using the specified boundaries.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array to crop.</param>
		/// <param name="options">The crop area boundaries.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A cropped PDF byte array.</returns>
		public static async Task<byte[]> CropAsync(
			this IPdfService service,
			byte[] pdfBytes,
			CropOptions options,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));
			if (options == null) throw new ArgumentNullException(nameof(options));

			using var sourceStream = new MemoryStream(pdfBytes);
			var croppedStream = await service.CropAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
			return await StreamToBytesAndDisposeAsync(croppedStream, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Removes the specified page numbers from a PDF document.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array.</param>
		/// <param name="pageNumbers">The 1-based page numbers to remove.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A PDF byte array without the removed pages.</returns>
		public static async Task<byte[]> RemovePagesAsync(
			this IPdfService service,
			byte[] pdfBytes,
			IEnumerable<int> pageNumbers,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));
			if (pageNumbers == null) throw new ArgumentNullException(nameof(pageNumbers));

			using var sourceStream = new MemoryStream(pdfBytes);
			var remainingStream = await service.RemovePagesAsync(sourceStream, pageNumbers, cancellationToken).ConfigureAwait(false);
			return await StreamToBytesAndDisposeAsync(remainingStream, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Performs Optical Character Recognition (OCR) on a PDF document.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array to OCR.</param>
		/// <param name="options">The OCR configuration options.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>An OCR result containing text and optional searchable PDF stream.</returns>
		public static async Task<OcrResult> OcrAsync(
			this IPdfService service,
			byte[] pdfBytes,
			OcrOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));

			using var sourceStream = new MemoryStream(pdfBytes);
			return await service.OcrAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Converts PDF pages into image byte arrays.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array to convert.</param>
		/// <param name="options">The image conversion settings.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A collection of image byte arrays, one for each page.</returns>
		public static async Task<IEnumerable<byte[]>> ConvertToImagesAsync(
			this IPdfService service,
			byte[] pdfBytes,
			ImageConversionOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));

			using var sourceStream = new MemoryStream(pdfBytes);
			var imageStreams = await service.ConvertToImagesAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
			var streamList = imageStreams.ToList();
			var results = new List<byte[]>();
			try
			{
				foreach (var stream in streamList)
				{
					results.Add(await StreamToBytesAndDisposeAsync(stream, cancellationToken).ConfigureAwait(false));
				}
				return results;
			}
			catch
			{
				foreach (var stream in streamList)
				{
					stream?.Dispose();
				}
				throw;
			}
		}

		/// <summary>
		/// Converts multiple image byte arrays into a single PDF document.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="imageBytesList">The collection of image byte arrays.</param>
		/// <param name="options">Options for page size and scaling.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A byte array containing the generated PDF.</returns>
		public static async Task<byte[]> ConvertToPdfAsync(
			this IPdfService service,
			IEnumerable<byte[]> imageBytesList,
			ImageToPdfOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (imageBytesList == null) throw new ArgumentNullException(nameof(imageBytesList));

			var streams = new List<MemoryStream>();
			try
			{
				foreach (var bytes in imageBytesList)
				{
					if (bytes == null) throw new ArgumentException("Image byte array collection contains a null reference.", nameof(imageBytesList));
					streams.Add(new MemoryStream(bytes));
				}

				var pdfStream = await service.ConvertToPdfAsync(streams, options, cancellationToken).ConfigureAwait(false);
				return await StreamToBytesAndDisposeAsync(pdfStream, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				foreach (var stream in streams)
				{
					stream.Dispose();
				}
			}
		}

		/// <summary>
		/// Extracts all readable text from a PDF document.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array to extract text from.</param>
		/// <param name="options">Options for text extraction.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The extracted text.</returns>
		public static async Task<string> ConvertToTextAsync(
			this IPdfService service,
			byte[] pdfBytes,
			TextExtractionOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));

			using var sourceStream = new MemoryStream(pdfBytes);
			return await service.ConvertToTextAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Converts raw text to a PDF document.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="text">The raw text to convert.</param>
		/// <param name="options">The text and formatting options.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A PDF byte array containing the generated PDF.</returns>
		public static async Task<byte[]> ConvertTextToPdfAsync(
			this IPdfService service,
			string text,
			TextToPdfOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (text == null) throw new ArgumentNullException(nameof(text));

			var pdfStream = await service.ConvertTextToPdfAsync(text, options, cancellationToken).ConfigureAwait(false);
			return await StreamToBytesAndDisposeAsync(pdfStream, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the document metadata of a PDF.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array.</param>
		/// <param name="password">The PDF owner or user password if protected.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The PDF document metadata.</returns>
		public static async Task<PdfMetadata> GetMetadataAsync(
			this IPdfService service,
			byte[] pdfBytes,
			string? password = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));

			using var sourceStream = new MemoryStream(pdfBytes);
			return await service.GetMetadataAsync(sourceStream, password, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Sets the document metadata of a PDF and returns the updated PDF byte array.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array.</param>
		/// <param name="metadata">The metadata values to apply.</param>
		/// <param name="password">The PDF owner password if protected.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The updated PDF byte array.</returns>
		public static async Task<byte[]> SetMetadataAsync(
			this IPdfService service,
			byte[] pdfBytes,
			PdfMetadata metadata,
			string? password = null,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));
			if (metadata == null) throw new ArgumentNullException(nameof(metadata));

			using var sourceStream = new MemoryStream(pdfBytes);
			var outputStream = await service.SetMetadataAsync(sourceStream, metadata, password, cancellationToken).ConfigureAwait(false);
			return await StreamToBytesAndDisposeAsync(outputStream, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Encrypts and protects a PDF document using passwords and permissions.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array to protect.</param>
		/// <param name="options">The protection and encryption options.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The encrypted PDF byte array.</returns>
		public static async Task<byte[]> ProtectAsync(
			this IPdfService service,
			byte[] pdfBytes,
			ProtectionOptions options,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));
			if (options == null) throw new ArgumentNullException(nameof(options));

			using var sourceStream = new MemoryStream(pdfBytes);
			var outputStream = await service.ProtectAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
			return await StreamToBytesAndDisposeAsync(outputStream, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Decrypts a protected PDF document and removes its passwords/permissions.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The encrypted PDF byte array.</param>
		/// <param name="password">The owner or user password required to decrypt.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The decrypted PDF byte array.</returns>
		public static async Task<byte[]> UnprotectAsync(
			this IPdfService service,
			byte[] pdfBytes,
			string password,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));
			if (password == null) throw new ArgumentNullException(nameof(password));

			using var sourceStream = new MemoryStream(pdfBytes);
			var outputStream = await service.UnprotectAsync(sourceStream, password, cancellationToken).ConfigureAwait(false);
			return await StreamToBytesAndDisposeAsync(outputStream, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Adds a text watermark to the pages of a PDF document.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array to watermark.</param>
		/// <param name="options">The watermark options.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The watermarked PDF byte array.</returns>
		public static async Task<byte[]> AddWatermarkAsync(
			this IPdfService service,
			byte[] pdfBytes,
			WatermarkOptions options,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));
			if (options == null) throw new ArgumentNullException(nameof(options));

			using var sourceStream = new MemoryStream(pdfBytes);
			var outputStream = await service.AddWatermarkAsync(sourceStream, options, cancellationToken).ConfigureAwait(false);
			return await StreamToBytesAndDisposeAsync(outputStream, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Extracts specific page numbers from a PDF document.
		/// </summary>
		/// <param name="service">The PDF service instance.</param>
		/// <param name="pdfBytes">The PDF byte array.</param>
		/// <param name="pageNumbers">The 1-based page numbers to extract.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A PDF byte array containing only the extracted pages.</returns>
		public static async Task<byte[]> ExtractPagesAsync(
			this IPdfService service,
			byte[] pdfBytes,
			IEnumerable<int> pageNumbers,
			CancellationToken cancellationToken = default)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (pdfBytes == null) throw new ArgumentNullException(nameof(pdfBytes));
			if (pageNumbers == null) throw new ArgumentNullException(nameof(pageNumbers));

			using var sourceStream = new MemoryStream(pdfBytes);
			var outputStream = await service.ExtractPagesAsync(sourceStream, pageNumbers, cancellationToken).ConfigureAwait(false);
			return await StreamToBytesAndDisposeAsync(outputStream, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the searchable PDF as a byte array from the OCR result.
		/// </summary>
		/// <param name="ocrResult">The OCR result.</param>
		/// <returns>A byte array containing the searchable PDF document, or an empty byte array if not applicable.</returns>
		public static byte[] GetSearchablePdfBytes(this OcrResult ocrResult)
		{
			if (ocrResult == null) throw new ArgumentNullException(nameof(ocrResult));
			if (ocrResult.SearchablePdfStream == null) return Array.Empty<byte>();

			if (ocrResult.SearchablePdfStream is MemoryStream ms)
			{
				return ms.ToArray();
			}

			using (var msTemp = new MemoryStream())
			{
				if (ocrResult.SearchablePdfStream.CanSeek && ocrResult.SearchablePdfStream.Position != 0)
				{
					ocrResult.SearchablePdfStream.Position = 0;
				}
				ocrResult.SearchablePdfStream.CopyTo(msTemp);
				return msTemp.ToArray();
			}
		}
	}
}
