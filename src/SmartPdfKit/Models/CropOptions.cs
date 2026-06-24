namespace SmartPdfKit.Models
{
	/// <summary>
	/// Options for cropping PDF pages.
	/// </summary>
	public class CropOptions
	{
		/// <summary>
		/// Gets or sets the left boundary.
		/// </summary>
		public double Left { get; set; }

		/// <summary>
		/// Gets or sets the top boundary.
		/// </summary>
		public double Top { get; set; }

		/// <summary>
		/// Gets or sets the right boundary.
		/// </summary>
		public double Right { get; set; }

		/// <summary>
		/// Gets or sets the bottom boundary.
		/// </summary>
		public double Bottom { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the coordinates are percentages (0-100) instead of absolute points.
		/// </summary>
		public bool UsePercentage { get; set; }

		/// <summary>
		/// Gets or sets the optional password for password-protected PDF files.
		/// </summary>
		public string? Password { get; set; }
	}
}
