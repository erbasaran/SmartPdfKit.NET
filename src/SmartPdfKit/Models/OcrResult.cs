using System;
using System.IO;

namespace SmartPdfKit.Models
{
	/// <summary>
	/// Holds the results of an OCR operation.
	/// </summary>
	public class OcrResult : IDisposable
	{
		/// <summary>
		/// Gets the extracted plain text.
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Gets the mean confidence score of the OCR process (0 to 1).
		/// </summary>
		public float Confidence { get; }

		/// <summary>
		/// Gets the stream containing the searchable PDF. This is only populated if the OCR mode was <see cref="OcrMode.SearchablePdf"/>.
		/// The caller is responsible for disposing this stream.
		/// </summary>
		public Stream? SearchablePdfStream { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OcrResult"/> class.
		/// </summary>
		/// <param name="text">The text extracted from the document.</param>
		/// <param name="confidence">The confidence score (0 to 1).</param>
		/// <param name="searchablePdfStream">An optional PDF stream containing searchable text.</param>
		public OcrResult(string text, float confidence, Stream? searchablePdfStream = null)
		{
			Text = text ?? string.Empty;
			Confidence = confidence;
			SearchablePdfStream = searchablePdfStream;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			SearchablePdfStream?.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
