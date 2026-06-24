namespace SmartPdfKit.Models
{
	/// <summary>
	/// Options for performing OCR on a PDF.
	/// </summary>
	public class OcrOptions
	{
		/// <summary>
		/// Gets or sets the target language code. Default is "eng" (English).
		/// Multiple languages can be separated by '+' (e.g. "eng+deu").
		/// </summary>
		public string Language { get; set; } = "eng";

		/// <summary>
		/// Gets or sets the path to the directory containing Tesseract language data files (.traineddata).
		/// If null, the system default location or environment variables will be used.
		/// </summary>
		public string? TessDataPath { get; set; }

		/// <summary>
		/// Gets or sets the OCR mode. Default is <see cref="OcrMode.TextOnly"/>.
		/// </summary>
		public OcrMode Mode { get; set; } = OcrMode.TextOnly;

		/// <summary>
		/// Gets or sets the DPI to render PDF pages at prior to running OCR. Default is 150.
		/// Higher DPI improves OCR accuracy but consumes more memory and processing time.
		/// </summary>
		public int Dpi { get; set; } = 150;

		/// <summary>
		/// Gets or sets the optional password for password-protected PDF files.
		/// </summary>
		public string? Password { get; set; }
	}
}
