using System;
using System.Text;

namespace Aspose.Imaging.Foss.Tests;

// Covers headers that are valid but unusual, or malformed in ways that used to leak
// zero/negative/garbage values into ImageInfo instead of reporting them as unknown.
public class HeaderCorrectnessTests
{
    [Fact]
    public void Png_DimensionAboveIntMax_IsReportedAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.Png(unchecked((int)0x80000000), 5));

        Assert.Equal(ImageFormat.Png, info.Format);
        Assert.Null(info.Width);
        Assert.Equal(5, info.Height);
    }

    [Fact]
    public void Png_ZeroWidth_IsReportedAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.Png(0, 5));

        Assert.Null(info.Width);
        Assert.Equal(5, info.Height);
    }

    [Fact]
    public void Png_TruncatedInsideIhdr_ReportsFormatOnly()
    {
        var info = ImageProbe.Probe(SampleImages.Png(640, 480)[..20]);

        Assert.Equal(ImageFormat.Png, info.Format);
        Assert.Null(info.Width);
        Assert.Null(info.Height);
        Assert.Null(info.BitDepth);
    }

    [Fact]
    public void Png_FirstChunkIsNotIhdr_ReportsFormatOnly()
    {
        var data = SampleImages.Png(640, 480);
        Encoding.ASCII.GetBytes("tEXt").CopyTo(data, 12);

        var info = ImageProbe.Probe(data);

        Assert.Equal(ImageFormat.Png, info.Format);
        Assert.Null(info.Width);
        Assert.Null(info.Height);
    }

    [Fact]
    public void Jpeg_TruncatedInsideStartOfFrame_ReportsFormatOnly()
    {
        var info = ImageProbe.Probe(SampleImages.Jpeg(100, 50)[..7]);

        Assert.Equal(ImageFormat.Jpeg, info.Format);
        Assert.Null(info.Width);
        Assert.Null(info.Height);
    }

    [Fact]
    public void Jpeg_ZeroHeightDeferredToDnl_ReportsHeightAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.Jpeg(100, 0));

        Assert.Equal(100, info.Width);
        Assert.Null(info.Height);
    }

    [Fact]
    public void Bmp_Os2CoreHeader_ReportsDimensionsAndBitCount()
    {
        var info = ImageProbe.Probe(SampleImages.BmpCore(300, 200, 8));

        Assert.Equal(ImageFormat.Bmp, info.Format);
        Assert.Equal(300, info.Width);
        Assert.Equal(200, info.Height);
        Assert.Equal(8, info.BitDepth);
    }

    [Theory]
    [InlineData(16u)]  // OS/2 2.x short form
    [InlineData(40u)]  // BITMAPINFOHEADER
    [InlineData(64u)]  // OS/2 2.x full form
    [InlineData(108u)] // BITMAPV4HEADER
    [InlineData(124u)] // BITMAPV5HEADER
    public void Bmp_InfoHeaderVariants_ReportDimensions(uint headerSize)
    {
        var info = ImageProbe.Probe(SampleImages.BmpWithHeaderSize(headerSize, 320, 240, 32));

        Assert.Equal(ImageFormat.Bmp, info.Format);
        Assert.Equal(320, info.Width);
        Assert.Equal(240, info.Height);
        Assert.Equal(32, info.BitDepth);
    }

    [Fact]
    public void Bmp_TextStartingWithBM_IsNotDetectedAsBmp()
    {
        var data = Encoding.ASCII.GetBytes("BMW owners club newsletter, issue 42");

        Assert.Equal(ImageFormat.Unknown, ImageProbe.DetectFormat(data));
        Assert.Equal(ImageFormat.Unknown, ImageProbe.Probe(data).Format);
    }

    [Fact]
    public void Bmp_HeightIntMinValue_IsReportedAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.Bmp(10, int.MinValue));

        Assert.Equal(ImageFormat.Bmp, info.Format);
        Assert.Equal(10, info.Width);
        Assert.Null(info.Height);
    }

    [Fact]
    public void Bmp_NegativeWidth_IsReportedAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.Bmp(-10, 20));

        Assert.Null(info.Width);
        Assert.Equal(20, info.Height);
    }

    [Fact]
    public void Bmp_TruncatedInfoHeader_ReportsFormatOnly()
    {
        var info = ImageProbe.Probe(SampleImages.Bmp(10, 20)[..24]);

        Assert.Equal(ImageFormat.Bmp, info.Format);
        Assert.Null(info.Width);
        Assert.Null(info.Height);
    }

    [Fact]
    public void Ico_ZeroBitCount_IsReportedAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.Ico(48, 48, bitCount: 0));

        Assert.Equal(48, info.Width);
        Assert.Null(info.BitDepth);
    }

    [Fact]
    public void Ico_NoImages_ReportsFormatOnly()
    {
        var info = ImageProbe.Probe(SampleImages.Ico(48, 48, imageCount: 0));

        Assert.Equal(ImageFormat.Ico, info.Format);
        Assert.Null(info.Width);
        Assert.Null(info.FrameCount);
    }

    [Fact]
    public void Tiff_LongDimensionAboveIntMax_IsReportedAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.TiffWithEntries(new[]
        {
            (256, 4, 1u, 0x80000000u),
            (257, 4, 1u, 600u),
        }));

        Assert.Equal(ImageFormat.Tiff, info.Format);
        Assert.Null(info.Width);
        Assert.Equal(600, info.Height);
    }

    [Fact]
    public void Tiff_RgbBitsPerSampleStoredOutOfLine_ReportsPerChannelDepth()
    {
        // Three SHORTs (6 bytes) don't fit the 4-byte value field, so it holds an offset instead.
        const int entryCount = 3;
        var info = ImageProbe.Probe(SampleImages.TiffWithEntries(
            new[]
            {
                (256, 3, 1u, 800u),
                (257, 3, 1u, 600u),
                (258, 3, 3u, SampleImages.TiffTrailerOffset(entryCount)),
            },
            trailer: new byte[] { 8, 0, 8, 0, 8, 0 }));

        Assert.Equal(800, info.Width);
        Assert.Equal(600, info.Height);
        Assert.Equal(8, info.BitDepth);
    }

    [Fact]
    public void Tiff_UnsupportedValueType_IsReportedAsUnknown()
    {
        // ASCII (type 2) is not a valid type for ImageWidth.
        var info = ImageProbe.Probe(SampleImages.TiffWithEntries(new[]
        {
            (256, 2, 1u, 800u),
            (257, 3, 1u, 600u),
        }));

        Assert.Null(info.Width);
        Assert.Equal(600, info.Height);
    }

    [Fact]
    public void Psd_ZeroWidth_IsReportedAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.Psd(0, 100));

        Assert.Null(info.Width);
        Assert.Equal(100, info.Height);
    }

    [Fact]
    public void Psd_TruncatedHeader_ReportsFormatOnly()
    {
        var info = ImageProbe.Probe(SampleImages.Psd(100, 100)[..20]);

        Assert.Equal(ImageFormat.Psd, info.Format);
        Assert.Null(info.Width);
        Assert.Null(info.Height);
    }

    [Fact]
    public void WebP_LossyWithoutStartCode_ReportsFormatOnly()
    {
        var data = SampleImages.WebPLossy(400, 300);
        data[23] = 0;

        var info = ImageProbe.Probe(data);

        Assert.Equal(ImageFormat.WebP, info.Format);
        Assert.Null(info.Width);
    }

    [Theory]
    [InlineData("lossy")]
    [InlineData("lossless")]
    [InlineData("extended")]
    public void WebP_TruncatedBitstreamHeader_ReportsFormatOnly(string variant)
    {
        var data = variant switch
        {
            "lossy" => SampleImages.WebPLossy(400, 300),
            "lossless" => SampleImages.WebPLossless(400, 300),
            _ => SampleImages.WebPExtended(400, 300),
        };

        var info = ImageProbe.Probe(data[..^2]);

        Assert.Equal(ImageFormat.WebP, info.Format);
        Assert.Null(info.Width);
        Assert.Null(info.Height);
    }

    [Fact]
    public void Emf_SinglePixelBounds_IsOneByOne()
    {
        var info = ImageProbe.Probe(SampleImages.EmfWithBounds(5, 5, 5, 5));

        Assert.Equal(1, info.Width);
        Assert.Equal(1, info.Height);
    }

    [Fact]
    public void Emf_EmptyBounds_AreReportedAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.EmfWithBounds(0, 0, -1, -1));

        Assert.Equal(ImageFormat.Emf, info.Format);
        Assert.Null(info.Width);
        Assert.Null(info.Height);
    }

    [Fact]
    public void Emf_BoundsSpanningWholeIntRange_AreReportedAsUnknownInsteadOfOverflowing()
    {
        var info = ImageProbe.Probe(SampleImages.EmfWithBounds(int.MinValue, 0, int.MaxValue, 9));

        Assert.Null(info.Width);
        Assert.Equal(10, info.Height);
    }

    [Fact]
    public void Wmf_SwappedBoundingBoxCorners_ReportPositiveSize()
    {
        var info = ImageProbe.Probe(SampleImages.WmfWithBounds(left: 3000, top: 1500, right: 0, bottom: 0, inch: 1440));

        Assert.Equal(200, info.Width);
        Assert.Equal(100, info.Height);
    }

    [Fact]
    public void Gif_ZeroScreenWidth_IsReportedAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.Gif(0, 10));

        Assert.Null(info.Width);
        Assert.Equal(10, info.Height);
    }

    [Fact]
    public void Avif_GenericMif1MajorBrandWithAvifCompatible_IsDetectedAsAvif()
    {
        var data = SampleImages.Heif("mif1", new[] { "mif1", "avif", "miaf" }, new[] { (64u, 32u) });

        Assert.Equal(ImageFormat.Avif, ImageProbe.DetectFormat(data));

        var info = ImageProbe.Probe(data);
        Assert.Equal(ImageFormat.Avif, info.Format);
        Assert.Equal(64, info.Width);
        Assert.Equal(32, info.Height);
    }

    [Fact]
    public void Heic_GenericMif1MajorBrandWithoutAvif_IsDetectedAsHeic()
    {
        var data = SampleImages.Heif("mif1", new[] { "mif1", "heic" }, new[] { (64u, 32u) });

        Assert.Equal(ImageFormat.Heic, ImageProbe.DetectFormat(data));
    }

    [Fact]
    public void Heic_CodecSpecificMajorBrand_WinsOverCompatibleAvif()
    {
        var data = SampleImages.Heif("heic", new[] { "mif1", "avif" }, new[] { (64u, 32u) });

        Assert.Equal(ImageFormat.Heic, ImageProbe.DetectFormat(data));
    }

    [Fact]
    public void Heic_GridImage_ReportsPrimaryItemSizeNotTileSize()
    {
        // iPhone-style layout: the tile ispe (index 2) precedes the grid ispe (index 3).
        // Item 1 is the grid (primary); items 2 and 3 are tiles sharing hvcC + tile ispe.
        var data = SampleImages.Heif(
            "heic", new[] { "mif1", "heic" },
            ispes: new[] { (512u, 512u), (4032u, 3024u) },
            primaryItemId: 1,
            associations: new[] { (1u, new[] { 3 }), (2u, new[] { 1, 2 }), (3u, new[] { 1, 2 }) });

        var info = ImageProbe.Probe(data);

        Assert.Equal(ImageFormat.Heic, info.Format);
        Assert.Equal(4032, info.Width);
        Assert.Equal(3024, info.Height);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1, false)]
    [InlineData(0, 0, true)]
    [InlineData(1, 1, true)]
    public void Heic_PrimaryItemAssociation_TakesPrecedenceOverLargestIspe(int pitmVersion, int ipmaVersion, bool wideIndices)
    {
        // The primary item is deliberately not the largest, so only a correct pitm/ipma walk passes.
        var data = SampleImages.Heif(
            "heic", Array.Empty<string>(),
            ispes: new[] { (4000u, 3000u), (1920u, 1080u) },
            primaryItemId: 7,
            associations: new[] { (5u, new[] { 2 }), (7u, new[] { 1, 3 }) },
            pitmVersion: pitmVersion,
            ipmaVersion: ipmaVersion,
            wideIpmaIndices: wideIndices);

        var info = ImageProbe.Probe(data);

        Assert.Equal(1920, info.Width);
        Assert.Equal(1080, info.Height);
    }

    [Fact]
    public void Heic_WithoutItemAssociations_FallsBackToLargestIspe()
    {
        var data = SampleImages.Heif("heic", Array.Empty<string>(), ispes: new[] { (512u, 512u), (4032u, 3024u), (320u, 240u) });

        var info = ImageProbe.Probe(data);

        Assert.Equal(4032, info.Width);
        Assert.Equal(3024, info.Height);
    }

    [Fact]
    public void Avif_IspeDimensionAboveIntMax_IsReportedAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.Avif(unchecked((int)0x80000000), 5));

        Assert.Equal(ImageFormat.Avif, info.Format);
        Assert.Null(info.Width);
        Assert.Equal(5, info.Height);
    }

    [Fact]
    public void DjVu_MultiPage_CountsPagesNotDirectoryFiles_AndUsesFirstPageSize()
    {
        var info = ImageProbe.Probe(SampleImages.DjVuMultiPage(320, 240, pageCount: 3));

        Assert.Equal(ImageFormat.DjVu, info.Format);
        Assert.Equal(3, info.FrameCount);
        Assert.Equal(320, info.Width);
        Assert.Equal(240, info.Height);
    }

    [Fact]
    public void DjVu_IndirectDocument_ReportsPageCountAsUnknown()
    {
        var info = ImageProbe.Probe(SampleImages.DjVuMultiPage(320, 240, pageCount: 3, bundled: false));

        Assert.Equal(ImageFormat.DjVu, info.Format);
        Assert.Null(info.FrameCount);
    }

    [Fact]
    public void DjVu_TruncatedBundledDocument_ReportsPageCountAsUnknown()
    {
        var full = SampleImages.DjVuMultiPage(320, 240, pageCount: 3);

        var info = ImageProbe.Probe(full[..^20]);

        Assert.Equal(ImageFormat.DjVu, info.Format);
        Assert.Null(info.FrameCount);
        Assert.Equal(320, info.Width);
    }
}
