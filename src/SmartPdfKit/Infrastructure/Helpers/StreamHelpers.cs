using System.IO;

namespace SmartPdfKit.Infrastructure.Helpers
{
	/// <summary>
	/// Internal stream helpers for handling streams within the infrastructure.
	/// </summary>
	internal static class StreamHelpers
	{
		/// <summary>
		/// Ensures the input stream is seekable and set to the beginning.
		/// If not seekable, copies it into a MemoryStream.
		/// </summary>
		public static Stream EnsureSeekable(Stream stream)
		{
			if (stream.CanSeek)
			{
				stream.Position = 0;
				return stream;
			}
			var ms = new MemoryStream();
			stream.CopyTo(ms);
			ms.Position = 0;
			return ms;
		}

		/// <summary>
		/// Safely converts a stream to a byte array.
		/// </summary>
		public static byte[] StreamToBytes(Stream stream)
		{
			if (stream.CanSeek)
			{
				stream.Position = 0;
			}
			if (stream is MemoryStream ms)
			{
				return ms.ToArray();
			}
			using (var msTemp = new MemoryStream())
			{
				stream.CopyTo(msTemp);
				return msTemp.ToArray();
			}
		}
	}
}
