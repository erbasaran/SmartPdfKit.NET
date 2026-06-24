using System;

namespace SmartPdfKit.Exceptions
{
	/// <summary>
	/// Exception thrown when a PDF cannot be parsed, read, or opened.
	/// </summary>
	public class PdfParsingException : PdfKitException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PdfParsingException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public PdfParsingException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="PdfParsingException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public PdfParsingException(string message, Exception innerException) : base(message, innerException) { }
	}
}
