using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartPdfKit.Models;

namespace SmartPdfKit.Interfaces
{
	/// <summary>
	/// Extensible provider interface for performing OCR on PDF documents.
	/// </summary>
	public interface IOcrProvider
	{
		/// <summary>
		/// Runs OCR on the pages of a PDF document.
		/// </summary>
		/// <param name="pdfStream">The PDF stream to process.</param>
		/// <param name="options">Options specifying the OCR configuration.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>An OCR result containing text and optional searchable PDF stream.</returns>
		Task<OcrResult> PerformOcrAsync(Stream pdfStream, OcrOptions options, CancellationToken cancellationToken = default);
	}
}
