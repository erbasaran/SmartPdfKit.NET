namespace SmartPdfKit.Models
{
	/// <summary>
	/// Modes for splitting a PDF document.
	/// </summary>
	public enum SplitMode
	{
		/// <summary>
		/// Splits the PDF into individual pages (1 page per output file).
		/// </summary>
		SplitAll,

		/// <summary>
		/// Splits the PDF by specific page ranges (e.g. "1-3, 5, 8-10").
		/// </summary>
		SplitByRanges,

		/// <summary>
		/// Splits the PDF into chunks of a fixed page size (e.g. 2 pages per chunk).
		/// </summary>
		SplitFixedSize
	}
}
