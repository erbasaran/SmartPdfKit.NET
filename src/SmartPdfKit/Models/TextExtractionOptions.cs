namespace SmartPdfKit.Models
{
	/// <summary>
	/// Options for extracting text from a PDF.
	/// </summary>
	public class TextExtractionOptions
	{
		/// <summary>
		/// Gets or sets the optional password for password-protected PDF files.
		/// </summary>
		public string? Password { get; set; }
	}
}
