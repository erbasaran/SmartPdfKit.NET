namespace SmartPdfKit.Models
{
	/// <summary>
	/// Options for splitting a PDF document.
	/// </summary>
	public class SplitOptions
	{
		/// <summary>
		/// Gets or sets the split mode. Default is <see cref="SplitMode.SplitAll"/>.
		/// </summary>
		public SplitMode Mode { get; set; } = SplitMode.SplitAll;

		/// <summary>
		/// Gets or sets the target page ranges. Only applicable when <see cref="Mode"/> is <see cref="SplitMode.SplitByRanges"/>.
		/// Example: "1-3, 5, 8-10" or "1-5".
		/// </summary>
		public string? Ranges { get; set; }

		/// <summary>
		/// Gets or sets the number of pages per split file. Only applicable when <see cref="Mode"/> is <see cref="SplitMode.SplitFixedSize"/>.
		/// Must be greater than 0.
		/// </summary>
		public int PageInterval { get; set; } = 1;

		/// <summary>
		/// Gets or sets the optional password for password-protected PDF files.
		/// </summary>
		public string? Password { get; set; }
	}
}
