using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartPdfKit.Models;

namespace SmartPdfKit.Interfaces
{
	/// <summary>
	/// Internal interface defining operations for rendering PDF pages as images.
	/// </summary>
	public interface IPdfRenderer
	{
		/// <summary>
		/// Renders PDF pages to image streams.
		/// </summary>
		/// <param name="pdfStream">The source PDF stream.</param>
		/// <param name="options">The image rendering options.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A collection of image streams, one for each page.</returns>
		Task<IEnumerable<Stream>> RenderToImagesAsync(Stream pdfStream, ImageConversionOptions options, CancellationToken cancellationToken = default);
	}
}
