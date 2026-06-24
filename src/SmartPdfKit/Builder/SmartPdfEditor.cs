using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartPdfKit.Interfaces;
using SmartPdfKit.Models;

namespace SmartPdfKit.Builder
{
	/// <summary>
	/// A fluent builder API to queue and execute multiple PDF operations in sequence.
	/// </summary>
	public class SmartPdfEditor : IDisposable
	{
		private Stream _currentStream;
		private readonly IPdfService _pdfService;
		private readonly List<Func<Stream, CancellationToken, Task<Stream>>> _pipeline = new();
		private readonly bool _ownsStream;

		private SmartPdfEditor(Stream sourceStream, IPdfService pdfService, bool ownsStream)
		{
			_currentStream = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));
			_pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
			_ownsStream = ownsStream;
		}

		/// <summary>
		/// Opens a PDF from a stream for fluent editing.
		/// </summary>
		/// <param name="sourceStream">The PDF source stream.</param>
		/// <param name="pdfService">The PDF service instance to execute operations.</param>
		public static SmartPdfEditor Open(Stream sourceStream, IPdfService pdfService)
		{
			return new SmartPdfEditor(sourceStream, pdfService, ownsStream: false);
		}

		/// <summary>
		/// Opens a PDF file from path for fluent editing.
		/// </summary>
		/// <param name="filePath">The path of the source PDF file.</param>
		/// <param name="pdfService">The PDF service instance to execute operations.</param>
		public static SmartPdfEditor Open(string filePath, IPdfService pdfService)
		{
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty.", nameof(filePath));
			var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			return new SmartPdfEditor(fileStream, pdfService, ownsStream: true);
		}

		/// <summary>
		/// Queues a rotation operation.
		/// </summary>
		public SmartPdfEditor Rotate(int degrees)
		{
			_pipeline.Add((stream, token) => _pdfService.RotateAsync(stream, degrees, token));
			return this;
		}

		/// <summary>
		/// Queues a crop operation.
		/// </summary>
		public SmartPdfEditor Crop(CropOptions options)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));
			_pipeline.Add((stream, token) => _pdfService.CropAsync(stream, options, token));
			return this;
		}

		/// <summary>
		/// Queues a page removal operation.
		/// </summary>
		public SmartPdfEditor RemovePages(IEnumerable<int> pageNumbers)
		{
			if (pageNumbers == null) throw new ArgumentNullException(nameof(pageNumbers));
			_pipeline.Add((stream, token) => _pdfService.RemovePagesAsync(stream, pageNumbers, token));
			return this;
		}

		/// <summary>
		/// Queues a compression operation.
		/// </summary>
		public SmartPdfEditor Compress(CompressOptions? options = null)
		{
			_pipeline.Add((stream, token) => _pdfService.CompressAsync(stream, options, token));
			return this;
		}

		/// <summary>
		/// Queues a watermark operation.
		/// </summary>
		public SmartPdfEditor AddWatermark(WatermarkOptions options)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));
			_pipeline.Add((stream, token) => _pdfService.AddWatermarkAsync(stream, options, token));
			return this;
		}

		/// <summary>
		/// Queues an encryption/protection operation.
		/// </summary>
		public SmartPdfEditor Protect(ProtectionOptions options)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));
			_pipeline.Add((stream, token) => _pdfService.ProtectAsync(stream, options, token));
			return this;
		}

		/// <summary>
		/// Queues a page extraction operation.
		/// </summary>
		public SmartPdfEditor ExtractPages(IEnumerable<int> pageNumbers)
		{
			if (pageNumbers == null) throw new ArgumentNullException(nameof(pageNumbers));
			_pipeline.Add((stream, token) => _pdfService.ExtractPagesAsync(stream, pageNumbers, token));
			return this;
		}

		/// <summary>
		/// Executes the pipeline of operations and returns the resulting PDF stream.
		/// </summary>
		public async Task<Stream> SaveAsync(CancellationToken cancellationToken = default)
		{
			Stream active = _currentStream;
			try
			{
				foreach (var step in _pipeline)
				{
					Stream next;
					try
					{
						next = await step(active, cancellationToken).ConfigureAwait(false);
					}
					catch
					{
						if (active != _currentStream)
						{
							active.Dispose();
						}
						throw;
					}

					// Dispose intermediate streams to save memory
					if (active != _currentStream)
					{
						active.Dispose();
					}
					active = next;
				}
				_pipeline.Clear();
				_currentStream = active;
				return active;
			}
			catch
			{
				_pipeline.Clear();
				throw;
			}
		}

		/// <summary>
		/// Executes the pipeline of operations and writes the resulting PDF to a file path.
		/// </summary>
		public async Task SaveAsync(string destinationPath, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path cannot be empty.", nameof(destinationPath));

			using var outputStream = await SaveAsync(cancellationToken).ConfigureAwait(false);
			using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await outputStream.CopyToAsync(fileStream, 81920, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_ownsStream)
			{
				_currentStream?.Dispose();
			}
			GC.SuppressFinalize(this);
		}
	}
}
