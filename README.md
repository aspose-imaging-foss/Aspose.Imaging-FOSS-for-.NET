# Aspose.Imaging FOSS for .NET

[![CI](https://github.com/aspose-imaging-foss/Aspose.Imaging-FOSS-for-.NET/actions/workflows/ci.yml/badge.svg)](https://github.com/aspose-imaging-foss/Aspose.Imaging-FOSS-for-.NET/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE) [![Contributors](https://img.shields.io/github/contributors/aspose-imaging-foss/Aspose.Imaging-FOSS-for-.NET.svg)](https://github.com/aspose-imaging-foss/Aspose.Imaging-FOSS-for-.NET/graphs/contributors)

[![Aspose.Imaging FOSS for .NET](https://products.aspose.org/media/imaging/net/banner-readme.png)](https://products.aspose.org/imaging/net/)

Aspose.Imaging FOSS for .NET is a free, open-source .NET library — an Aspose.Imaging
alternative that does zero-dependency image format detection and header probing. `ImageProbe`
identifies a file's format and reads its basic properties — dimensions, bit depth, and frame
count — directly from the file header, so you know what a file is and how big it is, fast,
without pulling in a full imaging stack to decode a single pixel. It has no NuGet dependencies
and targets `netstandard2.0` and `net8.0`.

## Navigation

- [At a Glance](#at-a-glance)
- [Key Capabilities](#key-capabilities)
- [Installation](#installation)
- [Dependencies](#dependencies)
- [Quick Start](#quick-start)
- [Additional Examples](#additional-examples)
- [API Reference](#api-reference)
- [Documentation & Resources](#documentation--resources)
- [Scope and Limitations](#scope-and-limitations)
- [Development and Testing](#development-and-testing)
- [License](#license)

## At a Glance

```mermaid
%%{init: {"flowchart": {"useMaxWidth": true}}}%%
flowchart TD
  subgraph StartingPoints["Starting Points"]
    direction TB
    i1["Image files<br/>(PNG, JPEG,<br/>GIF, BMP)"]
    i2["Image files<br/>(ICO, TIFF,<br/>WEBP)"]
    i3["Image files<br/>(PSD, EMF,<br/>WMF)"]
    i4["Image files<br/>(DICOM, DJVU,<br/>AVIF, HEIC)"]
  end
  PRODUCT["Aspose.Imaging FOSS for .NET"]
  subgraph Capabilities["Core Capabilities"]
    direction TB
    c1["Format detection,<br/>tolerant of<br/>bad input"]
    c2["Dimension,<br/>bit-depth,<br/>frame count"]
    c3["Sync/async<br/>probing"]
  end
  subgraph Outputs["Outputs"]
    direction TB
    o1["Images<br/>(ImageFormat,<br/>ImageInfo)"]
  end
  StartingPoints --> PRODUCT --> Capabilities --> Outputs
```

## Key Capabilities

- Detects 14 image formats as a free, open source Aspose.Imaging alternative to a full decode
  pipeline — `ImageProbe.DetectFormat()` reads only the file's signature bytes and returns an
  `ImageFormat` value, so identifying a file never requires pulling in a full imaging stack.
- `ImageProbe.Probe()` reads a format's real header fields directly into an `ImageInfo` —
  width, height, bit depth, and frame count — for PNG, JPEG, GIF, BMP, WEBP (lossy, lossless,
  and extended), ICO, TIFF, PSD, EMF, WMF, DICOM, DJVU, AVIF, and HEIC/HEIF, as a free open
  source .NET image manipulation API with no decode step.
- Counts real multi-frame and multi-page content where the container format actually carries
  it: GIF walks its image-descriptor blocks, ICO its directory entries, TIFF its IFD chain, and
  a bundled multi-page DJVU (DJVM) its `FORM:DJVU` page components — not the DIRM file count,
  which also includes shared dictionaries and thumbnails.
- DICOM parsing covers Implicit VR Little Endian and Explicit VR Little/Big Endian transfer
  syntaxes — including the common compressed ones, whose non-pixel elements are encoded
  Explicit VR Little Endian — and reads `Rows`/`Columns`/`BitsAllocated` directly from the data
  set, stopping rather than guessing if it reaches a sequence or encapsulated pixel data
  element with an undefined length first.
- AVIF and HEIC/HEIF dimensions come from the primary image's `ispe` (image spatial extent)
  property: `pitm` names the primary item and `ipma` links it to its `ipco` properties, so grid
  images (such as iPhone photos) report the full canvas rather than a tile. Without those
  associations the largest `ispe` is used. AVIF is detected by an `avif`/`avis` major brand, or
  by a compatible `avif`/`avis` brand under a generic major brand such as `mif1`; HEIC/HEIF by
  `heic`, `heix`, `hevc`, `hevx`, `heim`, `heis`, `hevm`, `hevs`, or `mif1`/`mif2` without AVIF.
- Every number in an `ImageInfo` is either positive or `null`: zero, negative, or out-of-range
  header fields are reported as unknown instead of being passed through.
- `Probe()` never throws on a signature match it can't fully parse — malformed or truncated
  input comes back as an `ImageInfo` with only `Format` set, never an exception, so a probing
  pipeline can run unattended over untrusted input.
- Both synchronous and async entry points exist for the same operation —
  `ProbeFile()`/`ProbeFileAsync()` and `Probe()`/`ProbeAsync()` — so a probe can run on an ASP.NET
  request thread without blocking it.

Most .NET image libraries make you fully decode an image just to answer "is this a JPEG, and
how big is it?". `ImageSharp.Image.Identify` covers common raster formats well, but nothing
else in the .NET FOSS ecosystem does the same for PSD, EMF/WMF, or DICOM — this library covers
that full range in one place.

| Format | Detect | Width / Height | Bit depth | Frame count |
|--------|:------:|:---------------:|:---------:|:-----------:|
| PNG    | ✅ | ✅ | ✅ | — |
| JPEG   | ✅ | ✅ | ✅ | — |
| GIF    | ✅ | ✅ | — | ✅ |
| BMP    | ✅ | ✅ | ✅ | — |
| WEBP (lossy / lossless / extended) | ✅ | ✅ | — | — |
| ICO    | ✅ | ✅ | ✅ | ✅ |
| TIFF   | ✅ | ✅ | ✅ | ✅ |
| PSD    | ✅ | ✅ | ✅ | — |
| EMF    | ✅ | ✅ (from bounds) | — | — |
| WMF (placeable) | ✅ | ✅ (assumes 96 DPI) | — | — |
| DICOM  | ✅ | ✅ (Rows/Columns) | ✅ (BitsAllocated) | — |
| DJVU (single-page / multi-page DJVM) | ✅ | ✅ | — | ✅ |
| AVIF (still / animated) | ✅ | ✅ | — | — |
| HEIC / HEIF | ✅ | ✅ | — | — |

Frame count is marked ✅ only where the container format genuinely records more than one
frame or page (GIF, ICO, TIFF, and multi-page DJVU); every other format's `FrameCount` comes
back as a fixed `1` on a successful parse, which this table does not count as a real frame
count.

## Installation

Build and install from a clone of the repository:

```bash
git clone https://github.com/aspose-imaging-foss/Aspose.Imaging-FOSS-for-.NET.git
cd Aspose.Imaging-FOSS-for-.NET
dotnet build src/Aspose.Imaging.Foss/Aspose.Imaging.Foss.csproj -c Release
```

Reference the built assembly from another project, or add the project directly:

```xml
<ProjectReference Include="..\Aspose.Imaging-FOSS-for-.NET\src\Aspose.Imaging.Foss\Aspose.Imaging.Foss.csproj" />
```

The library multi-targets `netstandard2.0` and `net8.0` — see
[Dependencies](#dependencies) below for the full required, native, and development-only
breakdown.

## Dependencies

### Required Package Dependencies

No required third-party package dependencies. The library builds from
`src/Aspose.Imaging.Foss/Aspose.Imaging.Foss.csproj`, which declares zero `<PackageReference>`
entries — every format's header parsing is implemented natively within the library.

### Native and System Requirements

- Targets `netstandard2.0` and `net8.0` (see [Installation](#installation)).

### Development Dependencies

- `Microsoft.NET.Test.Sdk` 17.12.0, `xunit` 2.9.2, and `xunit.runner.visualstudio` 2.8.2 — the
  xUnit test host and adapter used only by the test project.
- `coverlet.collector` 6.0.2 — code-coverage collection for the test suite.

None of these are referenced by the main library project; they apply only to
`tests/Aspose.Imaging.Foss.Tests/Aspose.Imaging.Foss.Tests.csproj` and are never shipped with
the library.

## Quick Start

```csharp
using Aspose.Imaging.Foss;

// From a file path
var info = ImageProbe.ProbeFile("photo.jpg");
Console.WriteLine($"{info.Format}: {info.Width}x{info.Height}, {info.BitDepth}-bit");

// From a stream (seekable or not) or a byte array
var format = ImageProbe.DetectFormat(bytes);
var info2 = ImageProbe.Probe(stream);
```

`Probe` never throws on malformed or truncated input — if a file matches a format's signature
but its header can't be fully parsed, you get back an `ImageInfo` with just the `Format` set
rather than an exception.

## Additional Examples

<details>
<summary>View Additional Examples</summary>

### Probe a File Asynchronously

`ProbeFileAsync()` and `ProbeAsync()` mirror their synchronous counterparts for use on a thread
that shouldn't block on I/O, such as an ASP.NET request handler:

```csharp
using Aspose.Imaging.Foss;

var info = await ImageProbe.ProbeFileAsync("upload.png", cancellationToken);
Console.WriteLine($"{info.Format}: {info.Width}x{info.Height}");
```

### Count Frames in a Multi-Frame GIF or a Multi-Page DJVU

`FrameCount` is only populated with a real count for formats whose container actually records
one — an animated GIF's image-descriptor blocks, or the page components of a bundled
multi-page DJVU (DJVM) document:

```csharp
using Aspose.Imaging.Foss;

var info = ImageProbe.ProbeFile("animation.gif");
Console.WriteLine($"{info.Format}: {info.FrameCount} frame(s)");
```

### Detect a Format From an Unseekable Stream

`DetectFormat` and `Probe` both buffer a non-seekable stream internally, so detection works the
same way over a network or compressed stream as it does over a file:

```csharp
using Aspose.Imaging.Foss;

using var networkStream = GetUploadStream(); // CanSeek == false
var format = ImageProbe.DetectFormat(networkStream);
```

</details>

## API Reference

`ImageProbe` is the library's single entry point; it returns an `ImageFormat` value and an
`ImageInfo` record for every supported format.

<details>
<summary>View the Full API Surface</summary>

#### Detailed Member Reference

- `ImageFormat` — enum of the 14 detectable formats, plus `Unknown` for anything unmatched.
- `ImageInfo` — immutable result of a probe: `Format`, and the nullable `Width`, `Height`,
  `BitDepth`, and `FrameCount` the format's header actually carried.
- `ImageProbe` — static entry point: `DetectFormat`, `Probe`, `ProbeFile`, `ProbeAsync`, and
  `ProbeFileAsync`.

- `ImageProbe.DetectFormat(Stream stream) -> ImageFormat` / `DetectFormat(byte[] data) -> ImageFormat`
  — signature-only match, no dimension parsing.
- `ImageProbe.Probe(Stream stream) -> ImageInfo` / `Probe(byte[] data) -> ImageInfo` / `Probe(FileInfo file) -> ImageInfo`
  — full header parse for whichever format's signature matches.
- `ImageProbe.ProbeFile(string path) -> ImageInfo` — opens the file and calls `Probe`.
- `ImageProbe.ProbeAsync(Stream stream, CancellationToken cancellationToken = default) -> Task<ImageInfo>` /
  `ProbeFileAsync(string path, CancellationToken cancellationToken = default) -> Task<ImageInfo>`
  — async counterparts of `Probe`/`ProbeFile`.
- `ImageInfo(ImageFormat format, int? width = null, int? height = null, int? bitDepth = null, int? frameCount = null)`
  — the only public constructor; every field besides `Format` is nullable because not every
  format's header carries all four.

</details>

## Documentation & Resources

- **[Getting started guide](https://docs.aspose.org/imaging/net/)** — installation and a
  first probe, walked through step by step.
- **[How-to guides & FAQ](https://kb.aspose.org/imaging/net/)** — task-focused answers for
  common format-detection questions.
- **[Full API reference](https://reference.aspose.org/imaging/net/)** — the complete browsable
  reference for `ImageFormat`, `ImageInfo`, and `ImageProbe`.
- Found a bug or have a feature request?
  [Open an issue](https://github.com/aspose-imaging-foss/Aspose.Imaging-FOSS-for-.NET/issues)
  on GitHub.

## Scope and Limitations

- **Detection and metadata only.** `ImageProbe` identifies a format and reads its header fields;
  it never decodes pixel data, so there is no rendering, editing, or format-conversion
  capability anywhere in the library.
- **Frame count reflects only what the container itself records.** It is a real, computed count
  for GIF, ICO, TIFF, and bundled multi-page DJVU (DJVM); every other supported format reports a
  fixed `1` for a successfully parsed header, since none of their headers carry a genuine frame
  or page count. An indirect (multi-file) DJVM document, or a truncated bundled one, reports
  `FrameCount` as `null`, since its pages can't all be seen from the one file.
- **Non-placeable WMF is detected but not dimensioned.** The classic (non-placeable) WMF header
  carries no bounding rectangle, so `Probe` returns a format-only `ImageInfo` for it; only the
  placeable WMF header (identified by its own magic number) yields `Width`/`Height`.
  Placeable-WMF dimensions assume a conventional 96 DPI target, since the header only stores
  units-per-inch bounds.
- **DICOM parsing stops at the first undefined-length element.** A sequence or encapsulated
  pixel-data element with an undefined length ends the walk rather than being parsed, since
  `Rows`/`Columns`/`BitsAllocated` always precede such elements in a conformant file.
- **CDR support is planned but not yet implemented** — no `CdrFormatHandler` exists in the
  current source tree.

These limitations apply only to Aspose.Imaging FOSS — they don't carry over to
[Aspose.Imaging for .NET — Enterprise Edition](https://products.aspose.com/imaging/net/), which
adds full decoding, editing, rendering, and conversion across a much broader format range.

## Development and Testing

Run the test suite from the repository root:

```bash
dotnet test tests/Aspose.Imaging.Foss.Tests/Aspose.Imaging.Foss.Tests.csproj
```

The test project targets `net9.0` and references the library via a plain `ProjectReference` —
no packaged dependency is required to build or test the library itself. A tagged
`v<major>.<minor>.<patch>` push runs [`.github/workflows/release.yml`](.github/workflows/release.yml),
which builds and publishes the package.

Adding a new format means implementing `IFormatHandler` (signature match + header parse) under
`src/Aspose.Imaging.Foss/Formats` and registering it in `ImageProbe`. Pull requests for new
formats or wider tag/chunk coverage on existing ones are welcome.

## License

This project is licensed under the [MIT License](LICENSE). The MIT License permits use,
copying, modification, distribution, sublicensing, and commercial use, provided its copyright
and permission notice are retained. The software is provided without warranty.
