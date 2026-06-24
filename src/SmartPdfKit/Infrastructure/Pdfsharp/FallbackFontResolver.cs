using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using PdfSharp.Fonts;

namespace SmartPdfKit.Infrastructure.Pdfsharp
{
	/// <summary>
	/// Fallback font resolver for PDFsharp Core. Finds system fonts on Windows and common Linux directories.
	/// </summary>
	public class FallbackFontResolver : IFontResolver
	{
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> _fontCache = new();

		/// <inheritdoc/>
		public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
		{
			string suffix = "";
			if (bold && italic) suffix = "bi";
			else if (bold) suffix = "bd";
			else if (italic) suffix = "i";

			string name = familyName.ToLowerInvariant();
			if (name == "arial" || name == "helvetica" || name == "sans-serif")
			{
				return new FontResolverInfo($"arial{suffix}");
			}

			// Standard fallback
			return new FontResolverInfo($"arial{suffix}");
		}

		/// <inheritdoc/>
		public byte[]? GetFont(string faceName)
		{
			string cacheKey = faceName.ToLowerInvariant();
			if (_fontCache.TryGetValue(cacheKey, out var cachedBytes))
			{
				return cachedBytes;
			}

			string fontFileName = faceName.ToLowerInvariant() switch
			{
				"arial" => "arial.ttf",
				"arialbd" => "arialbd.ttf",
				"ariali" => "ariali.ttf",
				"arialbi" => "arialbi.ttf",
				_ => "arial.ttf"
			};

			var paths = new List<string>();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", fontFileName));
			}
			else
			{
				// Linux common paths
				paths.Add(Path.Combine("/usr/share/fonts/truetype/dejavu", fontFileName.Replace("arial", "DejaVuSans")));
				paths.Add(Path.Combine("/usr/share/fonts/truetype/liberation", fontFileName.Replace("arial", "LiberationSans")));
				paths.Add(Path.Combine("/usr/share/fonts/truetype/msttcorefonts", fontFileName));
				paths.Add(Path.Combine("/usr/share/fonts", fontFileName));
			}

			foreach (var path in paths)
			{
				if (File.Exists(path))
				{
					try
					{
						byte[] bytes = File.ReadAllBytes(path);
						_fontCache[cacheKey] = bytes;
						return bytes;
					}
					catch
					{
						// Ignore and try next path
					}
				}
			}

			// Fallback: If no font is found on Linux, scan common dirs for ANY .ttf
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var fallbackDirs = new[] { "/usr/share/fonts", "/usr/local/share/fonts" };
				foreach (var dir in fallbackDirs)
				{
					if (Directory.Exists(dir))
					{
						try
						{
							var files = Directory.GetFiles(dir, "*.ttf", SearchOption.AllDirectories);
							if (files.Length > 0)
							{
								byte[] bytes = File.ReadAllBytes(files[0]);
								_fontCache[cacheKey] = bytes;
								return bytes;
							}
						}
						catch
						{
							// Ignore
						}
					}
				}
			}

			return null;
		}
	}
}
