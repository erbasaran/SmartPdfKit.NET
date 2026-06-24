# SmartPdfKit

[![NuGet Version](https://img.shields.io/nuget/v/SmartPdfKit.svg)](https://www.nuget.org/packages/SmartPdfKit)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**SmartPdfKit** is an enterprise-grade, production-ready, cross-platform PDF toolkit for .NET. Inspired by the extensive capabilities of Stirling-PDF, it is designed from the ground up for high-performance stream processing, thread safety, clean architecture, and low memory footprints.

It is fully compatible with **Windows and Linux** out-of-the-box, without requiring any platform-specific code modifications.

---

## Features

- 📑 **PDF Merge**: Combine multiple PDF files/streams into a single document.
- ✂️ **PDF Split**: Split PDFs into individual pages, fixed-size chunks, or custom page ranges.
- 🗜️ **PDF Compress**: Optimize content streams and strip metadata to minimize file sizes.
- 📐 **PDF Crop**: Crop page boundaries using absolute points or percentage-based margins.
- 🔄 **PDF Rotate**: Rotate pages in multiples of 90 degrees.
- 🔍 **PDF OCR**: Extract layout-accurate text and convert scanned PDFs into searchable PDFs (using Tesseract).
- 🖼️ **PDF ↔ Image Conversion**: Render PDF pages to PNG/JPEG images, and compile images back to PDF.
- 📝 **PDF ↔ Text Conversion**: Extract layout-accurate text from PDFs, and convert plain text (with Form-Feed `\f` page breaks) into formatted PDFs.
- ❌ **Remove Pages**: Delete specific pages from a document safely.
- ✍️ **Metadata Editing**: Read and write document information fields (Title, Author, Subject, Keywords, Creator, Creation/Modification dates).
- 🔒 **PDF Encryption & Decryption**: Secure PDF documents using User/Owner passwords and restrict permissions (printing, modifying, content copying).
- 🎨 **Text Watermarking**: Apply semi-transparent rotated text watermarks on top of PDF pages.
- 📂 **Page Extraction**: Extract specific page indices into a new PDF document.
- 🔗 **Fluent API**: Chain operations on PDF streams/files in a single readable pipeline.

---

## Supported Platforms & Prerequisites

SmartPdfKit is designed to be cross-platform, running on **Windows, Linux, and macOS**. Because it relies on some native engines (like PDFium for image rendering and Tesseract for OCR), please review the requirements below.

### 💻 Windows
Works **out-of-the-box** without any additional dependencies. All native binaries (`pdfium.dll`, etc.) are bundled and extracted automatically.

### 🐧 Linux & Docker
To run SmartPdfKit in Linux environments (or within Linux-based Docker containers), you must install native library packages for font handling, image rendering, and OCR:

```bash
# Update package manager and install native dependencies
sudo apt-get update && sudo apt-get install -y \
    libgdiplus \
    fontconfig \
    fonts-dejavu \
    fonts-liberation \
    tesseract-ocr \
    && rm -rf /var/lib/apt/lists/*
```

#### Copy-Paste Dockerfile Setup
If you are containerizing your C# application using .NET 8.0, 9.0, or 10.0, use the following template for your `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Install dependencies for fonts (PDFsharp text drawing) and Tesseract OCR
RUN apt-get update && apt-get install -y \
    libgdiplus \
    fontconfig \
    fonts-dejavu \
    fonts-liberation \
    tesseract-ocr \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# (Standard Build Steps...)
```

---

## Dependency Information

To prevent restrictive **AGPL licensing constraints** (associated with libraries like iText), SmartPdfKit uses only highly permissive **MIT** and **Apache 2.0** licensed dependencies:

1. **PDFsharp (6.1.1+)** [MIT]: Core PDF syntax and page structural operations.
2. **UglyToad.PdfPig (1.7.0+)** [MIT]: High-accuracy text layout extraction.
3. **Docnet.Core (2.3.1+)** [MIT]: Multi-platform PDFium rendering wrapper.
4. **SkiaSharp (4.148.0+)** [MIT]: High-performance cross-platform 2D graphics and image processing by Google. *(Ensures 100% free and open-source compliance without any commercial licensing risks)*.
5. **Tesseract (5.2.0+)** [Apache 2.0]: .NET Tesseract OCR engine wrapper.

---

## Installation

Install the library via NuGet:

```bash
dotnet add package SmartPdfKit
```

---

## Quick Start

### Method A: Register with Dependency Injection (Recommended)

In your `Program.cs` or startup configuration:

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Registers IPdfService, IPdfProcessor, IPdfRenderer, IPdfTextExtractor, IOcrProvider
services.AddSmartPdfKit(); 

var serviceProvider = services.BuildServiceProvider();
var pdfService = serviceProvider.GetRequiredService<IPdfService>();
```

### Method B: Manual Instantiation (Without Dependency Injection)

If you are not using a Dependency Injection container, you can instantiate the service directly:

```csharp
using SmartPdfKit.Services;
using SmartPdfKit.Infrastructure.Pdfsharp;
using SmartPdfKit.Infrastructure.Rendering;
using SmartPdfKit.Infrastructure.Text;
using SmartPdfKit.Infrastructure.Ocr;
using Microsoft.Extensions.Logging.Abstractions;

// 1. Instantiate the infrastructure processors
var processor = new PdfsharpProcessor();
var renderer = new DocnetRenderer();
var textExtractor = new PdfPigTextExtractor();
var ocrProvider = new TesseractOcrProvider(renderer);

// 2. Instantiate the orchestrator PdfService
var pdfService = new PdfService(
    processor, 
    renderer, 
    textExtractor, 
    NullLogger<PdfService>.Instance, 
    ocrProvider
);
```

---

## Full Usage Examples

Every operation is async-first and exposes two developer-friendly interfaces:
1. **High-Performance Streams**: Best for APIs, web apps, and memory-sensitive workloads.
2. **File Paths (String Overloads)**: Simple file-to-file utility extensions.

### 📑 PDF Merge
Combine multiple PDFs into a single document.

```csharp
// Option 1: Stream-Based (Recommended for Web/Cloud)
using var s1 = File.OpenRead("file1.pdf");
using var s2 = File.OpenRead("file2.pdf");
using var mergedStream = await pdfService.MergeAsync(new[] { s1, s2 });
using var output = File.Create("merged.pdf");
await mergedStream.CopyToAsync(output);

// Option 2: File-Path Based
var files = new[] { "file1.pdf", "file2.pdf" };
await pdfService.MergeAsync(files, "merged.pdf");
```

### ✂️ PDF Split
Split a PDF into individual 1-page files, fixed intervals, or custom page ranges.

```csharp
// Option 1: Stream-Based
using var sourceStream = File.OpenRead("large.pdf");
var splitStreams = await pdfService.SplitAsync(sourceStream, new SplitOptions
{
    Mode = SplitMode.SplitByRanges,
    Ranges = "1-2, 4-5",          // Split into two PDFs: pages 1-2, and pages 4-5
    PageInterval = 1,             // Ignored when mode is SplitByRanges
    Password = "optionalPassword" // Password if the source PDF is encrypted
});
// (Dispose splitStreams when done)

// Option 2: File-Path Based
await pdfService.SplitAsync("large.pdf", "output_directory", "page_{0}.pdf", new SplitOptions
{
    Mode = SplitMode.SplitFixedSize, // Split in fixed page intervals
    PageInterval = 2,                // Split into 2-page documents
    Ranges = null,                   // Ignored when mode is SplitFixedSize
    Password = null
});
```

### 🗜️ PDF Compress
Optimize content streams and strip metadata to minimize file sizes.

```csharp
var options = new CompressOptions 
{ 
    CompressContentStreams = true, 
    ImageQuality = 60,             // 0-100 quality for re-compressing PDF images
    MaxImageDimension = 1200,      // Resizes images larger than this width/height
    RemoveMetadata = true,         // Strips Adobe metadata objects losslessly
    Password = "optionalPassword"  // Password if the source PDF is encrypted
};

// Option 1: Stream-Based
using var source = File.OpenRead("large.pdf");
using var compressedStream = await pdfService.CompressAsync(source, options);

// Option 2: File-Path Based
await pdfService.CompressAsync("large.pdf", "compressed.pdf", options);
```

### 📐 PDF Crop
Crop page boundaries using absolute points or percentage-based margins.

```csharp
var cropOptions = new CropOptions 
{ 
    Left = 10, 
    Top = 10, 
    Right = 10, 
    Bottom = 10, 
    UsePercentage = true,          // If true, coordinates are percentages (0-100); if false, absolute points.
    Password = "optionalPassword"  // Password if the source PDF is encrypted
};

// Option 1: Stream-Based
using var source = File.OpenRead("input.pdf");
using var croppedStream = await pdfService.CropAsync(source, cropOptions);

// Option 2: File-Path Based
await pdfService.CropAsync("input.pdf", "cropped.pdf", cropOptions);
```

### 🔄 PDF Rotate
Rotate pages in multiples of 90 degrees.

```csharp
// Option 1: Stream-Based
using var source = File.OpenRead("input.pdf");
using var rotatedStream = await pdfService.RotateAsync(source, 90);

// Option 2: File-Path Based
await pdfService.RotateAsync("input.pdf", "rotated.pdf", 90);
```

### ❌ Remove Pages
Delete specific page indices from a PDF.

```csharp
var pagesToRemove = new[] { 2, 4 }; // Deletes the 2nd and 4th pages

// Option 1: Stream-Based
using var source = File.OpenRead("input.pdf");
using var outputStream = await pdfService.RemovePagesAsync(source, pagesToRemove);

// Option 2: File-Path Based
await pdfService.RemovePagesAsync("input.pdf", "output.pdf", pagesToRemove);
```

### 🖼️ PDF to Image Conversion
Render PDF pages into PNG or JPEG images.

```csharp
var options = new ImageConversionOptions 
{ 
    Format = ImageFormat.Jpeg,     // Convert PDF pages to Jpeg
    Dpi = 150,                     // DPI resolution
    Quality = 80,                  // JPEG compression quality (ignored for PNG)
    Password = "optionalPassword"  // Password if the source PDF is encrypted
};

// Option 1: Stream-Based
using var source = File.OpenRead("input.pdf");
var imageStreams = await pdfService.ConvertToImagesAsync(source, options);

// Option 2: File-Path Based
await pdfService.ConvertToImagesAsync("input.pdf", "images_folder", "page_{0}.jpg", options);
```

### 🖼️ Image to PDF Conversion
Compile multiple image files/streams back into a single PDF document.

```csharp
var options = new ImageToPdfOptions 
{ 
    AutoPageSize = true,           // Resize pages to fit individual images' dimensions
    Margin = 0                     // Page margins in points (1 inch = 72 points)
};

// Option 1: Stream-Based
using var img1 = File.OpenRead("img1.png");
using var img2 = File.OpenRead("img2.png");
using var pdfStream = await pdfService.ConvertToPdfAsync(new[] { img1, img2 }, options);

// Option 2: File-Path Based
var imagePaths = new[] { "img1.png", "img2.png" };
await pdfService.ConvertToPdfAsync(imagePaths, "output.pdf", options);
```

### 📝 Text to PDF Conversion
Convert plain text to a formatted PDF. Supports page-break characters (`\f`).

```csharp
string text = "This is Page 1\fThis is Page 2";
var options = new TextToPdfOptions { FontName = "Helvetica", FontSize = 12, Margin = 36 };

// Option 1: Stream-Based
using var pdfStream = await pdfService.ConvertTextToPdfAsync(text, options);

// Option 2: File-Path Based
await pdfService.ConvertTextToPdfAsync(text, "output.pdf", options);
```

### 📝 PDF to Text Extraction
Extract layout-accurate raw text from a PDF.

```csharp
// Option 1: Stream-Based
using var source = File.OpenRead("input.pdf");
string text = await pdfService.ConvertToTextAsync(source);

// Option 2: File-Path Based
string fileText = await pdfService.ConvertToTextAsync("input.pdf");
```

### 🔍 PDF OCR (Tesseract Engine)
Perform OCR on a scanned image PDF. Can extract plain text or export a searchable PDF stream containing embedded invisible text layers.

> [!IMPORTANT]
> **Tesseract Language Files (.traineddata)**
> - To run OCR, you must download the relevant Tesseract language files (`.traineddata` files, e.g. `eng.traineddata` for English).
> - You can download these files from: **[tesseract-ocr/tessdata_best](https://github.com/tesseract-ocr/tessdata_best)**
> - Place them in a directory (e.g., `tessdata`) and specify its path in `OcrOptions.TessDataPath`.
> - If no path is specified, the library checks the `TESSDATA_PREFIX` environment variable or searches for a directory named `tessdata` in your application base directory.

```csharp
using var source = File.OpenRead("scanned.pdf");
var result = await pdfService.OcrAsync(source, new OcrOptions
{
    Language = "eng", // Language dataset code (e.g. "eng", "tur", "eng+tur")
    Mode = OcrMode.SearchablePdf, // OCR and generate text-searchable PDF
    Dpi = 150,                    // Image DPI rendering before OCR
    TessDataPath = "/usr/share/tesseract-ocr/5/tessdata", // Path to folder containing .traineddata files
    Password = "optionalPassword" // Password if the source PDF is encrypted
});

Console.WriteLine($"Mean Confidence: {result.Confidence:P2}");
Console.WriteLine($"Extracted Text: {result.Text}");

if (result.SearchablePdfStream != null)
{
    using var output = File.Create("searchable.pdf");
    await result.SearchablePdfStream.CopyToAsync(output);
}
```

### ✍️ PDF Metadata Editing
Read and write document properties.

```csharp
// Option 1: Read/Write via Streams
using var source = File.OpenRead("input.pdf");
var metadata = await pdfService.GetMetadataAsync(source, "optionalPassword");
Console.WriteLine($"Title: {metadata.Title}, Author: {metadata.Author}, Producer: {metadata.Producer}");

var newMeta = new PdfMetadata 
{ 
    Title = "New Title", 
    Author = "Antigravity",
    Subject = "PDF Processing Guide",
    Keywords = "pdf, csharp, smartpdfkit",
    Creator = "My App",
    CreationDate = DateTime.Now,
    ModificationDate = DateTime.Now
};
using var updatedStream = await pdfService.SetMetadataAsync(source, newMeta, "optionalPassword");

// Option 2: Write via File Paths
await pdfService.SetMetadataAsync("input.pdf", "output.pdf", newMeta, "optionalPassword");
```

### 🔒 PDF Encryption & Decryption
Secure a PDF using User/Owner passwords and restrict usage permissions.

```csharp
var protectOptions = new ProtectionOptions
{
    UserPassword = "user123",  // Password needed to open/view the file
    OwnerPassword = "owner123", // Password needed to modify permissions
    PermitPrint = true,
    PermitModify = false,
    PermitCopy = false
};

// Option 1: Stream-Based
using var source = File.OpenRead("input.pdf");
using var protectedStream = await pdfService.ProtectAsync(source, protectOptions);
using var decryptedStream = await pdfService.UnprotectAsync(protectedStream, "owner123");

// Option 2: File-Path Based
await pdfService.ProtectAsync("input.pdf", "protected.pdf", protectOptions);
await pdfService.UnprotectAsync("protected.pdf", "decrypted.pdf", "owner123");
```

### 🎨 Text Watermarking
Apply semi-transparent, rotated text watermarks over the pages of a PDF.

```csharp
var options = new WatermarkOptions
{
    Text = "INTERNAL ONLY",
    FontName = "Helvetica",
    FontSize = 40,
    Opacity = 0.25,                // 25% opacity (0.0 to 1.0)
    Rotation = 45,                 // Rotation angle in degrees
    Password = "optionalPassword"  // Password if the source PDF is encrypted
};

// Option 1: Stream-Based
using var source = File.OpenRead("input.pdf");
using var watermarkedStream = await pdfService.AddWatermarkAsync(source, options);

// Option 2: File-Path Based
await pdfService.AddWatermarkAsync("input.pdf", "watermarked.pdf", options);
```

### 📂 Page Extraction
Extract specific page indices into a new, smaller PDF document.

```csharp
var pagesToExtract = new[] { 1, 3 }; // Extracts page 1 and page 3

// Option 1: Stream-Based
using var source = File.OpenRead("input.pdf");
using var extractedStream = await pdfService.ExtractPagesAsync(source, pagesToExtract);

// Option 2: File-Path Based
await pdfService.ExtractPagesAsync("input.pdf", "extracted.pdf", pagesToExtract);
```

### 🔗 Fluent Builder API (`SmartPdfEditor`)
Queue and execute multiple actions in a clean, readable pipeline.

```csharp
using SmartPdfKit.Builder;

var cropOptions = new CropOptions { Left = 5, Top = 5, Right = 5, Bottom = 5, UsePercentage = true };

// Option 1: Stream-Based (Input & Output Streams)
using var source = File.OpenRead("input.pdf");
using var editor = SmartPdfEditor.Open(source, pdfService);
using var resultStream = await editor
    .Rotate(180)
    .Crop(cropOptions)
    .Compress()
    .SaveAsync(); // Returns optimized output stream

// Option 2: File-Path Based
using var editorPath = SmartPdfEditor.Open("input.pdf", pdfService);
await editorPath
    .Rotate(180)
    .Crop(cropOptions)
    .Compress()
    .SaveAsync("fluent_output.pdf"); // Saves directly to disk
```

---

## Error Handling

All exception structures inherit from `PdfKitException`, making error isolation simple:

- **`PdfParsingException`**: Thrown if the PDF document is corrupt, contains invalid structural elements, or cannot be opened due to password requirements.
- **`PdfProcessingException`**: Thrown if an operation fails (e.g., trying to rotate by a non-90-degree angle, specifying invalid crop margins, or attempting to remove all pages from a document).
- **`PdfOcrException`**: Thrown if Tesseract is missing, language resources (`.traineddata`) are not found, or native OCR engines crash.

```csharp
try
{
    await pdfService.RotateAsync("input.pdf", "output.pdf", 45); // Will fail (45 is not a multiple of 90)
}
catch (PdfProcessingException ex)
{
    Console.WriteLine($"Processing error: {ex.Message}");
}
catch (PdfKitException ex)
{
    Console.WriteLine($"Toolkit error: {ex.Message}");
}
```

---

## API Configuration & Options Reference

### 🗜️ CompressOptions
| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `CompressContentStreams` | `bool` | `true` | Compress page text content streams using FlateDecode. |
| `ImageQuality` | `int?` | `75` | JPEG quality (0-100) for re-compressing images. Null skips image compression. |
| `MaxImageDimension` | `int` | `1000` | Downscales images larger than this width/height. |
| `RemoveMetadata` | `bool` | `false` | Strips metadata blocks, private application headers, and page thumbnails. |
| `Password` | `string?` | `null` | Optional password for opening encrypted/protected PDFs. |

### 📐 CropOptions
| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Left` | `double` | `0` | Margins/bounds coordinates. |
| `Top` | `double` | `0` | Margins/bounds coordinates. |
| `Right` | `double` | `0` | Margins/bounds coordinates. |
| `Bottom` | `double` | `0` | Margins/bounds coordinates. |
| `UsePercentage` | `bool` | `false` | If true, bounds are percentages (0-100); if false, they are points. |
| `Password` | `string?` | `null` | Optional password to decrypt the source PDF. |

### ✂️ SplitOptions
| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Mode` | `SplitMode` | `SplitAll` | Split strategy: `SplitAll`, `SplitFixedSize`, or `SplitByRanges`. |
| `Ranges` | `string?` | `null` | Target page ranges (e.g. `"1-3, 5, 8-10"`), used when `Mode` is `SplitByRanges`. |
| `PageInterval` | `int` | `1` | Number of pages per split file, used when `Mode` is `SplitFixedSize`. |
| `Password` | `string?` | `null` | Optional password to decrypt the source PDF. |

### 🔍 OcrOptions
| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Language` | `string` | `"eng"` | Tesseract language code (e.g. `"eng"`, `"tur"`, or combined like `"eng+tur"`). |
| `TessDataPath` | `string?` | `null` | Custom directory path where `.traineddata` files are stored. |
| `Mode` | `OcrMode` | `TextOnly` | Mode of operation: `TextOnly` (extract text only) or `SearchablePdf` (generate PDF with OCR text overlay). |
| `Dpi` | `int` | `150` | DPI at which PDF pages are rendered prior to OCR. |
| `Password` | `string?` | `null` | Optional password to decrypt the source PDF. |

### 🖼️ ImageConversionOptions
| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Format` | `ImageFormat` | `Png` | Target image format: `Png` or `Jpeg`. |
| `Dpi` | `int` | `150` | Resolution for rendering PDF pages. |
| `Quality` | `int` | `80` | JPEG compression quality (0-100); ignored for PNG. |
| `Password` | `string?` | `null` | Optional password to decrypt the source PDF. |

### 🖼️ ImageToPdfOptions
| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Margin` | `double` | `0` | Page margin in points (1 inch = 72 points). |
| `AutoPageSize` | `bool` | `true` | If true, PDF pages will resize to match original image dimensions. If false, standard A4 is used. |

### 📝 TextToPdfOptions
| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `FontName` | `string` | `"Helvetica"` | Name of font used for drawing text. |
| `FontSize` | `double` | `12` | Font size in points. |
| `Margin` | `double` | `36` | Page margins in points. |

### 🎨 WatermarkOptions
| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `Text` | `string` | `"CONFIDENTIAL"` | Watermark text content. |
| `FontName` | `string` | `"Helvetica"` | Font used for watermark. |
| `FontSize` | `double` | `48` | Font size in points. |
| `Opacity` | `double` | `0.3` | Text opacity from `0.0` (fully transparent) to `1.0` (fully opaque). |
| `Rotation` | `double` | `45` | Angle of text rotation in degrees. |
| `Password` | `string?` | `null` | Optional password to decrypt the source PDF. |

### 🔒 ProtectionOptions
| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `UserPassword` | `string?` | `null` | Password required to open and view the PDF. |
| `OwnerPassword` | `string?` | `null` | Password required to change permissions or decrypt the document. |
| `PermitPrint` | `bool` | `true` | Allow printing of the PDF. |
| `PermitModify` | `bool` | `true` | Allow editing/modifying the PDF content. |
| `PermitCopy` | `bool` | `true` | Allow content copying and extraction. |

### ✍️ PdfMetadata
| Property | Type | Description |
| :--- | :--- | :--- |
| `Title` | `string?` | Document title. |
| `Author` | `string?` | Document author. |
| `Subject` | `string?` | Document subject. |
| `Keywords` | `string?` | Document keywords. |
| `Creator` | `string?` | Creator application. |
| `Producer` | `string?` | PDF engine/producer. |
| `CreationDate` | `DateTime?` | Creation date. |
| `ModificationDate` | `DateTime?` | Last modification date. |

---

## Performance Recommendations

1. **Prefer Streams**: Always use the stream-based API in web or high-throughput applications. It avoids loading entire files into memory arrays and plays nicely with DI container lifecycles.
2. **Buffer Copier**: When writing output streams to disk or web contexts, use buffers. The path-based extension overloads use `CopyToAsync` with an optimized `80KB` buffer size by default.
3. **OCR Configuration**: Limit the OCR resolution (`Dpi` option in `OcrOptions`) to `150` or `300` DPI. Rendering pages at higher resolutions drastically increases memory allocation and slows down Tesseract processing.
4. **Dispose Resources**: Keep `OcrResult` wrapped in `using` blocks because generating searchable PDFs yields a temporary stream that needs to be disposed.
5. **Cross-Platform Fonts**: PDFsharp Core build does not load OS fonts on Linux by default. SmartPdfKit automatically initializes a fallback resolver scanning `/usr/share/fonts/` for true-type fonts to prevent rendering crashes, but we recommend specifying standard, widely available fonts (like `Helvetica` or `Arial`) or registering your own custom fonts in production Docker environments.

---

## License

This project is licensed under the **MIT License**.
