namespace SmartPdfKit.Models
{
	/// <summary>
	/// Options for converting PDF pages to images.
	/// </summary>
	public class ImageConversionOptions
	{
		/// <summary>
		/// Gets or sets the output image format. Default is <see cref="ImageFormat.Png"/>.
		/// </summary>
		public ImageFormat Format { get; set; } = ImageFormat.Png;

		/// <summary>
		/// Gets or sets the DPI for rendering PDF pages. Default is 150.
		/// </summary>
		public int Dpi { get; set; } = 150;

		/// <summary>
		/// Gets or sets the JPEG compression quality (0-100). Only applicable when <see cref="Format"/> is <see cref="ImageFormat.Jpeg"/>. Default is 80.
		/// </summary>
		public int Quality { get; set; } = 80;

		/// <summary>
		/// Gets or sets the optional password for password-protected PDF files.
		/// </summary>
		public string? Password { get; set; }
	}
}
