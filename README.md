# Aspose.Imaging FOSS for .NET

[![CI](https://github.com/aspose-imaging-foss/Aspose.Imaging.Foss/actions/workflows/ci.yml/badge.svg)](https://github.com/aspose-imaging-foss/Aspose.Imaging.Foss/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Aspose.Imaging.Foss.svg)](https://www.nuget.org/packages/Aspose.Imaging.Foss)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Zero-dependency image format detection and header probing for .NET.

`Aspose.Imaging.Foss` identifies an image's format and reads its basic properties
(dimensions, bit depth, frame count) directly from the file header — without
decoding a single pixel. It's built for cases where you need to know **what**
a file is and **how big** it is, fast, without pulling in a full imaging stack.

It has no NuGet dependencies and targets `netstandard2.0` and `net8.0`.

## Why this exists

Most .NET image libraries make you fully decode an image just to answer "is
this a JPEG, and how big is it?". `ImageSharp.Image.Identify` covers common
raster formats well, but nothing in the .NET FOSS ecosystem does the same for
PSD, EMF/WMF, or DICOM. This library covers that full range in one place.

If you need to actually **decode, edit, or convert** any of these formats,
that's a different job — see [Aspose.Imaging](https://products.aspose.com/imaging/net/)
for a full-featured commercial SDK covering all of these formats and more.

## Supported formats

| Format | Detect | Width / Height | Bit depth | Frame count |
|--------|:------:|:---------------:|:---------:|:-----------:|
| PNG    | ✅ | ✅ | ✅ | ✅ |
| JPEG   | ✅ | ✅ | ✅ | — |
| GIF    | ✅ | ✅ | — | ✅ |
| BMP    | ✅ | ✅ | ✅ | — |
| WebP (lossy / lossless / extended) | ✅ | ✅ | — | — |
| ICO    | ✅ | ✅ | ✅ | ✅ |
| TIFF   | ✅ | ✅ | ✅ | ✅ |
| PSD    | ✅ | ✅ | ✅ | — |
| EMF    | ✅ | ✅ (from bounds) | — | — |
| WMF (placeable) | ✅ | ✅ (assumes 96 DPI) | — | — |
| DICOM  | ✅ | ✅ (Rows/Columns) | ✅ (BitsAllocated) | — |
| DjVu (single-page DJVU / multi-page DJVM) | ✅ | ✅ | — | ✅ |
| AVIF (still / animated) | ✅ | ✅ | — | — |
| HEIC / HEIF | ✅ | ✅ | — | — |

DICOM parsing covers Implicit VR Little Endian and Explicit VR Little/Big Endian transfer
syntaxes (including the common compressed ones, whose non-pixel elements are Explicit VR
Little Endian); it stops rather than guesses if it hits a sequence or encapsulated pixel data
with an undefined length before finding Rows/Columns/BitsAllocated.

DjVu reads dimensions and frame count from the IFF chunk tree; multi-page files (DJVM) report
the page count from the DIRM directory chunk and dimensions from the first embedded DJVU page.

AVIF and HEIC/HEIF dimensions are read from the `ispe` (image spatial extent) property box
via the `meta → iprp → ipco → ispe` path. Brand detection covers `avif`/`avis` for AVIF and
`heic`, `heix`, `hevc`, `hevx`, `mif1`/`mif2` for HEIC/HEIF.

Planned: CDR.

## Install

```
dotnet add package Aspose.Imaging.Foss
```

## Usage

```csharp
using Aspose.Imaging.Foss;

// From a file path
var info = ImageProbe.ProbeFile("photo.jpg");
Console.WriteLine($"{info.Format}: {info.Width}x{info.Height}, {info.BitDepth}-bit");

// From a stream (seekable or not) or a byte array
var format = ImageProbe.DetectFormat(bytes);
var info2 = ImageProbe.Probe(stream);
```

`Probe` never throws on malformed or truncated input — if a file matches a
format's signature but its header can't be fully parsed, you get back an
`ImageInfo` with just the `Format` set rather than an exception.

## Contributing

Adding a new format means implementing `IFormatHandler` (signature match +
header parse) under `src/Aspose.Imaging.Foss/Formats` and registering it in
`ImageProbe`. Pull requests for new formats or wider tag/chunk coverage on
existing ones are welcome.

## License

MIT — see [LICENSE](LICENSE).
