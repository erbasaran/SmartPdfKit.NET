using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartPdfKit.Models;

namespace SmartPdfKit.Interfaces
{
	/// <summary>
	/// Internal interface defining low-level PDF editing and creation operations.
	/// </summary>
	public interface IPdfProcessor
	{
		/// <summary>
		/// Merges multiple PDF streams into a single PDF.
		/// </summary>
		Task<Stream> MergeAsync(IEnumerable<Stream> pdfStreams, CancellationToken cancellationToken = default);

		/// <summary>
		/// Splits a single PDF stream into multiple.
		/// </summary>
		Task<IEnumerable<Stream>> SplitAsync(Stream pdfStream, SplitOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Compresses content streams of a PDF.
		/// </summary>
		Task<Stream> CompressAsync(Stream pdfStream, CompressOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Rotates the pages of a PDF document by the specified degrees.
		/// </summary>
		Task<Stream> RotateAsync(Stream pdfStream, int rotationDegrees, CancellationToken cancellationToken = default);

		/// <summary>
		/// Crops the pages of a PDF document using the specified options.
		/// </summary>
		Task<Stream> CropAsync(Stream pdfStream, CropOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Removes pages from a PDF.
		/// </summary>
		Task<Stream> RemovePagesAsync(Stream pdfStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default);

		/// <summary>
		/// Converts raw text to a PDF stream.
		/// </summary>
		Task<Stream> TextToPdfAsync(string text, TextToPdfOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Converts multiple images to a single PDF stream.
		/// </summary>
		Task<Stream> ImageToPdfAsync(IEnumerable<Stream> imageStreams, ImageToPdfOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the document metadata of a PDF.
		/// </summary>
		Task<PdfMetadata> GetMetadataAsync(Stream pdfStream, string? password = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Sets the document metadata of a PDF.
		/// </summary>
		Task<Stream> SetMetadataAsync(Stream pdfStream, PdfMetadata metadata, string? password = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Encrypts and protects a PDF.
		/// </summary>
		Task<Stream> ProtectAsync(Stream pdfStream, ProtectionOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Decrypts a protected PDF.
		/// </summary>
		Task<Stream> UnprotectAsync(Stream pdfStream, string password, CancellationToken cancellationToken = default);

		/// <summary>
		/// Adds a text watermark to PDF pages.
		/// </summary>
		Task<Stream> AddWatermarkAsync(Stream pdfStream, WatermarkOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Extracts specific page numbers from a PDF.
		/// </summary>
		Task<Stream> ExtractPagesAsync(Stream pdfStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default);
	}
}
