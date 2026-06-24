using System;

namespace SmartPdfKit.Exceptions
{
	/// <summary>
	/// Exception thrown when an OCR operation fails due to missing dependencies, languages, or engine errors.
	/// </summary>
	public class PdfOcrException : PdfKitException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PdfOcrException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public PdfOcrException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="PdfOcrException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public PdfOcrException(string message, Exception innerException) : base(message, innerException) { }
	}
}
