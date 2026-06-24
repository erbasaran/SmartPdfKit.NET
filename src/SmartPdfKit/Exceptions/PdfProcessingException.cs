using System;

namespace SmartPdfKit.Exceptions
{
	/// <summary>
	/// Exception thrown when a PDF manipulation operation fails.
	/// </summary>
	public class PdfProcessingException : PdfKitException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PdfProcessingException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public PdfProcessingException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="PdfProcessingException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public PdfProcessingException(string message, Exception innerException) : base(message, innerException) { }
	}
}
