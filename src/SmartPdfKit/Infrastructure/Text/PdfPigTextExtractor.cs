using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmartPdfKit.Exceptions;
using SmartPdfKit.Interfaces;
using SmartPdfKit.Models;
using UglyToad.PdfPig;

namespace SmartPdfKit.Infrastructure.Text
{
	/// <summary>
	/// Implements <see cref="IPdfTextExtractor"/> using UglyToad.PdfPig.
	/// </summary>
	public class PdfPigTextExtractor : IPdfTextExtractor
	{
		/// <inheritdoc/>
		public async Task<string> ExtractTextAsync(Stream pdfStream, TextExtractionOptions options, CancellationToken cancellationToken = default)
		{
			if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
			if (options == null) throw new ArgumentNullException(nameof(options));

			return await Task.Run(() =>
			{
				try
				{
					var seekable = EnsureSeekable(pdfStream);
					try
					{
						var sb = new StringBuilder();

						var parsingOptions = new ParsingOptions();
						if (!string.IsNullOrEmpty(options.Password))
						{
							parsingOptions.Password = options.Password;
						}

						using (var document = PdfDocument.Open(seekable, parsingOptions))
						{
							foreach (var page in document.GetPages())
							{
								cancellationToken.ThrowIfCancellationRequested();
								sb.AppendLine(page.Text);
							}
						}

						return sb.ToString();
					}
					finally
					{
						if (seekable != pdfStream)
						{
							seekable.Dispose();
						}
					}
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					throw new PdfParsingException("Failed to extract text from PDF document.", ex);
				}
			}, cancellationToken).ConfigureAwait(false);
		}

		private static Stream EnsureSeekable(Stream stream) => Helpers.StreamHelpers.EnsureSeekable(stream);
	}
}
