using System;

namespace SmartPdfKit.Models
{
	/// <summary>
	/// Represents the document metadata of a PDF.
	/// </summary>
	public class PdfMetadata
	{
		/// <summary>
		/// Gets or sets the document title.
		/// </summary>
		public string? Title { get; set; }

		/// <summary>
		/// Gets or sets the document author.
		/// </summary>
		public string? Author { get; set; }

		/// <summary>
		/// Gets or sets the document subject.
		/// </summary>
		public string? Subject { get; set; }

		/// <summary>
		/// Gets or sets the keywords.
		/// </summary>
		public string? Keywords { get; set; }

		/// <summary>
		/// Gets or sets the creator application.
		/// </summary>
		public string? Creator { get; set; }

		/// <summary>
		/// Gets or sets the PDF producer.
		/// </summary>
		public string? Producer { get; set; }

		/// <summary>
		/// Gets or sets the creation date.
		/// </summary>
		public DateTime? CreationDate { get; set; }

		/// <summary>
		/// Gets or sets the modification date.
		/// </summary>
		public DateTime? ModificationDate { get; set; }
	}
}
