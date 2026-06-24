namespace SmartPdfKit.Models
{
	/// <summary>
	/// Options for compressing a PDF document.
	/// </summary>
	public class CompressOptions
	{
		/// <summary>
		/// Gets or sets a value indicating whether content streams should be compressed (FlateDecode). Default is true.
		/// </summary>
		public bool CompressContentStreams { get; set; } = true;

		/// <summary>
		/// Gets or sets the image quality (0 to 100) for re-compressing PDF images, if supported.
		/// Null means keep original quality or do not compress images.
		/// </summary>
		public int? ImageQuality { get; set; } = 75;

		/// <summary>
		/// Gets or sets a value indicating whether unused objects and metadata should be stripped. Default is false.
		/// </summary>
		public bool RemoveMetadata { get; set; } = false;

		/// <summary>
		/// Gets or sets the maximum dimension (width or height) in pixels for images.
		/// Images larger than this will be downscaled. Default is 1000.
		/// </summary>
		public int MaxImageDimension { get; set; } = 1000;

		/// <summary>
		/// Gets or sets the optional password for password-protected PDF files.
		/// </summary>
		public string? Password { get; set; }
	}
}
