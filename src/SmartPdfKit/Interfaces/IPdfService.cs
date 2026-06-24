using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartPdfKit.Models;

namespace SmartPdfKit.Interfaces
{
	/// <summary>
	/// User-facing service interface for all PDF operations.
	/// </summary>
	public interface IPdfService
	{
		/// <summary>
		/// Merges multiple PDF streams into a single PDF stream.
		/// </summary>
		/// <param name="pdfStreams">The collection of PDF streams to merge.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A new memory stream containing the merged PDF.</returns>
		Task<Stream> MergeAsync(IEnumerable<Stream> pdfStreams, CancellationToken cancellationToken = default);

		/// <summary>
		/// Splits a single PDF stream into multiple PDF streams based on split options.
		/// </summary>
		/// <param name="pdfStream">The PDF stream to split.</param>
		/// <param name="options">Options specifying how to split the PDF.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A collection of streams, each representing a split PDF file.</returns>
		Task<IEnumerable<Stream>> SplitAsync(Stream pdfStream, SplitOptions? options = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Compresses a PDF stream to reduce its file size.
		/// </summary>
		/// <param name="pdfStream">The PDF stream to compress.</param>
		/// <param name="options">Compression options.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A compressed PDF stream.</returns>
		Task<Stream> CompressAsync(Stream pdfStream, CompressOptions? options = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Rotates the pages of a PDF document by a specified degree.
		/// </summary>
		/// <param name="pdfStream">The PDF stream to rotate.</param>
		/// <param name="rotationDegrees">The rotation angle in degrees (must be a multiple of 90, e.g. 90, 180, 270).</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A rotated PDF stream.</returns>
		Task<Stream> RotateAsync(Stream pdfStream, int rotationDegrees, CancellationToken cancellationToken = default);

		/// <summary>
		/// Crops the pages of a PDF document using the specified boundaries.
		/// </summary>
		/// <param name="pdfStream">The PDF stream to crop.</param>
		/// <param name="options">The crop area boundaries.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A cropped PDF stream.</returns>
		Task<Stream> CropAsync(Stream pdfStream, CropOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Removes the specified page numbers from a PDF document.
		/// </summary>
		/// <param name="pdfStream">The PDF stream.</param>
		/// <param name="pageNumbers">The 1-based page numbers to remove.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A PDF stream without the removed pages.</returns>
		Task<Stream> RemovePagesAsync(Stream pdfStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default);

		/// <summary>
		/// Performs Optical Character Recognition (OCR) on a PDF document.
		/// </summary>
		/// <param name="pdfStream">The PDF stream to OCR.</param>
		/// <param name="options">The OCR configuration options.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>An OCR result containing text and optional searchable PDF stream.</returns>
		Task<OcrResult> OcrAsync(Stream pdfStream, OcrOptions? options = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Converts PDF pages into image streams.
		/// </summary>
		/// <param name="pdfStream">The PDF stream to convert.</param>
		/// <param name="options">The image conversion settings.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A collection of image streams, one for each page.</returns>
		Task<IEnumerable<Stream>> ConvertToImagesAsync(Stream pdfStream, ImageConversionOptions? options = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Converts multiple images into a single PDF document.
		/// </summary>
		/// <param name="imageStreams">The collection of image streams.</param>
		/// <param name="options">Options for page size and scaling.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A stream containing the generated PDF.</returns>
		Task<Stream> ConvertToPdfAsync(IEnumerable<Stream> imageStreams, ImageToPdfOptions? options = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Extracts all readable text from a PDF document.
		/// </summary>
		/// <param name="pdfStream">The PDF stream to extract text from.</param>
		/// <param name="options">Options for text extraction.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The extracted text.</returns>
		Task<string> ConvertToTextAsync(Stream pdfStream, TextExtractionOptions? options = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Converts raw text to a PDF document.
		/// </summary>
		/// <param name="text">The raw text to convert.</param>
		/// <param name="options">The text and formatting options.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A stream containing the generated PDF.</returns>
		Task<Stream> ConvertTextToPdfAsync(string text, TextToPdfOptions? options = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the document metadata of a PDF.
		/// </summary>
		Task<PdfMetadata> GetMetadataAsync(Stream pdfStream, string? password = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Sets the document metadata of a PDF and returns the updated stream.
		/// </summary>
		Task<Stream> SetMetadataAsync(Stream pdfStream, PdfMetadata metadata, string? password = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Encrypts and protects a PDF document using passwords and permissions.
		/// </summary>
		Task<Stream> ProtectAsync(Stream pdfStream, ProtectionOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Decrypts a protected PDF document and removes its passwords/permissions.
		/// </summary>
		Task<Stream> UnprotectAsync(Stream pdfStream, string password, CancellationToken cancellationToken = default);

		/// <summary>
		/// Adds a text watermark to the pages of a PDF document.
		/// </summary>
		Task<Stream> AddWatermarkAsync(Stream pdfStream, WatermarkOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Extracts specific page numbers from a PDF document.
		/// </summary>
		Task<Stream> ExtractPagesAsync(Stream pdfStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default);
	}
}
