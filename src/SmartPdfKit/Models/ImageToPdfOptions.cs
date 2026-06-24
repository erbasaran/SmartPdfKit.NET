namespace SmartPdfKit.Models
{
	/// <summary>
	/// Options for converting images to a PDF.
	/// </summary>
	public class ImageToPdfOptions
	{
		/// <summary>
		/// Gets or sets the page margin in points (1 inch = 72 points). Default is 0.
		/// </summary>
		public double Margin { get; set; } = 0;

		/// <summary>
		/// Gets or sets a value indicating whether each page should resize to fit the image's original dimensions.
		/// If false, standard A4 page size will be used and images will be scaled to fit. Default is true.
		/// </summary>
		public bool AutoPageSize { get; set; } = true;
	}
}
