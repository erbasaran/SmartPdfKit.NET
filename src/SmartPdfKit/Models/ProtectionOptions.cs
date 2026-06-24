namespace SmartPdfKit.Models
{
	/// <summary>
	/// Options for encrypting and protecting a PDF.
	/// </summary>
	public class ProtectionOptions
	{
		/// <summary>
		/// Gets or sets the password required to open the PDF.
		/// </summary>
		public string? UserPassword { get; set; }

		/// <summary>
		/// Gets or sets the password required to edit permissions or decrypt the file.
		/// </summary>
		public string? OwnerPassword { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether printing is permitted. Default is true.
		/// </summary>
		public bool PermitPrint { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether modifying content is permitted. Default is true.
		/// </summary>
		public bool PermitModify { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether copying content is permitted. Default is true.
		/// </summary>
		public bool PermitCopy { get; set; } = true;
	}
}
