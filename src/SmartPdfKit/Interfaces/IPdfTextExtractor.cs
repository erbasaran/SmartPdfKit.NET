using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartPdfKit.Models;

namespace SmartPdfKit.Interfaces
{
	/// <summary>
	/// Internal interface defining operations for extracting text from a PDF.
	/// </summary>
	public interface IPdfTextExtractor
	{
		/// <summary>
		/// Extracts plain text from the PDF document.
		/// </summary>
		/// <param name="pdfStream">The PDF stream to extract text from.</param>
		/// <param name="options">Options for text extraction.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The extracted text.</returns>
		Task<string> ExtractTextAsync(Stream pdfStream, TextExtractionOptions options, CancellationToken cancellationToken = default);
	}
}
