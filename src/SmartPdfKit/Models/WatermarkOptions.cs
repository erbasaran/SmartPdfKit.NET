namespace SmartPdfKit.Models
{
	/// <summary>
	/// Options for applying a text watermark to PDF pages.
	/// </summary>
	public class WatermarkOptions
	{
		/// <summary>
		/// Gets or sets the watermark text. Default is "CONFIDENTIAL".
		/// </summary>
		public string Text { get; set; } = "CONFIDENTIAL";

		/// <summary>
		/// Gets or sets the font name. Default is "Helvetica".
		/// </summary>
		public string FontName { get; set; } = "Helvetica";

		/// <summary>
		/// Gets or sets the font size in points. Default is 48.
		/// </summary>
		public double FontSize { get; set; } = 48;

		/// <summary>
		/// Gets or sets the text opacity (0.0 to 1.0). Default is 0.3 (30%).
		/// </summary>
		public double Opacity { get; set; } = 0.3;

		/// <summary>
		/// Gets or sets the text rotation in degrees. Default is 45.
		/// </summary>
		public double Rotation { get; set; } = 45;

		/// <summary>
		/// Gets or sets the optional password for password-protected PDF files.
		/// </summary>
		public string? Password { get; set; }
	}
}
