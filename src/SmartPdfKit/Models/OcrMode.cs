namespace SmartPdfKit.Models
{
	/// <summary>
	/// Modes for running OCR on a PDF.
	/// </summary>
	public enum OcrMode
	{
		/// <summary>
		/// Extracts plain text only.
		/// </summary>
		TextOnly,

		/// <summary>
		/// Generates a searchable PDF document containing the images with OCR text overlaid.
		/// </summary>
		SearchablePdf
	}
}
