namespace SmartPdfKit.Models
{
	/// <summary>
	/// Options for converting raw text to a PDF.
	/// </summary>
	public class TextToPdfOptions
	{
		/// <summary>
		/// Gets or sets the font name. Default is "Helvetica".
		/// </summary>
		public string FontName { get; set; } = "Helvetica";

		/// <summary>
		/// Gets or sets the font size in points. Default is 12.
		/// </summary>
		public double FontSize { get; set; } = 12;

		/// <summary>
		/// Gets or sets the page margin in points. Default is 36 (0.5 inches).
		/// </summary>
		public double Margin { get; set; } = 36;
	}
}
