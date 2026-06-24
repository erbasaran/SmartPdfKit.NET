using SmartPdfKit.Infrastructure.Ocr;
using SmartPdfKit.Infrastructure.Pdfsharp;
using SmartPdfKit.Infrastructure.Rendering;
using SmartPdfKit.Infrastructure.Text;
using SmartPdfKit.Interfaces;
using SmartPdfKit.Services;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Contains extension methods for configuring SmartPdfKit dependency injection.
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Registers SmartPdfKit core and infrastructure services in the dependency injection container.
		/// </summary>
		/// <param name="services">The service collection.</param>
		/// <returns>The updated service collection.</returns>
		public static IServiceCollection AddSmartPdfKit(this IServiceCollection services)
		{
			// Register infrastructure engines
			services.AddSingleton<IPdfProcessor, PdfsharpProcessor>();
			services.AddSingleton<IPdfRenderer, DocnetRenderer>();
			services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
			services.AddSingleton<IOcrProvider, TesseractOcrProvider>();

			// Register core user-facing service
			services.AddSingleton<IPdfService, PdfService>();

			return services;
		}
	}
}
