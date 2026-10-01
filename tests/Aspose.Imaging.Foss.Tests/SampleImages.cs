namespace Aspose.Imaging.Foss.Tests;

internal static class SampleImages
{
    public static byte[] Png(int width, int height, int bitDepth = 8) =>
        new ByteBuilder()
            .Bytes(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
            .UInt32BE(13)
            .Ascii("IHDR")
            .UInt32BE((uint)width)
            .UInt32BE((uint)height)
            .Bytes((byte)bitDepth, 2, 0, 0, 0)
            .Zeros(4)
            .ToArray();

    public static byte[] Jpeg(int width, int height, int precision = 8) =>
        new ByteBuilder()
            .Bytes(0xFF, 0xD8, 0xFF, 0xC0)
            .UInt16BE(7)
            .Bytes((byte)precision)
            .UInt16BE(height)
            .UInt16BE(width)
            .ToArray();

    public static byte[] TruncatedJpeg() => new byte[] { 0xFF, 0xD8, 0xFF };

    public static byte[] Gif(int width, int height) => GifWithFrames(width, height, frameCount: 1);

    public static byte[] GifWithFrames(int width, int height, int frameCount)
    {
        var builder = new ByteBuilder()
            .Ascii("GIF89a")
            .UInt16LE(width)
            .UInt16LE(height)
            .Bytes(0, 0, 0); // packed fields (no global color table), bg color index, aspect ratio

        for (var i = 0; i < frameCount; i++)
        {
            builder
                .Bytes(ImageDescriptorIntroducer)
                .UInt16LE(0).UInt16LE(0) // left, top
                .UInt16LE(width).UInt16LE(height)
                .Bytes(0) // packed fields (no local color table)
                .Bytes(2) // LZW minimum code size
                .Bytes(1, 0x00, 0); // one data sub-block, then the terminator
        }

        builder.Bytes(GifTrailer);
        return builder.ToArray();
    }

    public static byte[] GifWithoutFrames(int width, int height) =>
        new ByteBuilder()
            .Ascii("GIF89a")
            .UInt16LE(width)
            .UInt16LE(height)
            .Bytes(0, 0, 0)
            .ToArray();

    private const byte ImageDescriptorIntroducer = 0x2C;
    private const byte GifTrailer = 0x3B;

    public static byte[] Bmp(int width, int height, int bitCount = 24) =>
        new ByteBuilder()
            .Ascii("BM")
            .UInt32LE(0)
            .UInt32LE(0)
            .UInt32LE(54)
            .UInt32LE(40)
            .Int32LE(width)
            .Int32LE(height)
            .UInt16LE(1)
            .UInt16LE(bitCount)
            .ToArray();

    public static byte[] WebPLossy(int width, int height) =>
        new ByteBuilder()
            .Ascii("RIFF")
            .UInt32LE(0)
            .Ascii("WEBP")
            .Ascii("VP8 ")
            .UInt32LE(10)
            .Bytes(0, 0, 0)
            .Bytes(0x9d, 0x01, 0x2a)
            .UInt16LE(width & 0x3FFF)
            .UInt16LE(height & 0x3FFF)
            .ToArray();

    public static byte[] WebPLossless(int width, int height)
    {
        var packed = (uint)((width - 1) & 0x3FFF) | (uint)(((height - 1) & 0x3FFF) << 14);
        return new ByteBuilder()
            .Ascii("RIFF")
            .UInt32LE(0)
            .Ascii("WEBP")
            .Ascii("VP8L")
            .UInt32LE(5)
            .Bytes(0x2F)
            .UInt32LE(packed)
            .ToArray();
    }

    public static byte[] WebPExtended(int width, int height) =>
        new ByteBuilder()
            .Ascii("RIFF")
            .UInt32LE(0)
            .Ascii("WEBP")
            .Ascii("VP8X")
            .UInt32LE(10)
            .Bytes(0)
            .Zeros(3)
            .UInt24LE(width - 1)
            .UInt24LE(height - 1)
            .ToArray();

    public static byte[] Ico(int width, int height, int bitCount = 32, int imageCount = 1) =>
        new ByteBuilder()
            .Bytes(0, 0, 1, 0)
            .UInt16LE(imageCount)
            .Bytes((byte)width, (byte)height, 0, 0)
            .UInt16LE(1)
            .UInt16LE(bitCount)
            .UInt32LE(0)
            .UInt32LE(22)
            .ToArray();

    public static byte[] Tiff(int width, int height, int bitDepth = 8) =>
        TiffWithPages(width, height, bitDepth, pageCount: 1);

    public static byte[] TiffWithPages(int width, int height, int bitDepth, int pageCount)
    {
        const int entryCount = 3;
        const int ifdSize = 2 + entryCount * 12 + 4; // count + entries + next-IFD offset
        const int firstIfdOffset = 8;

        var builder = new ByteBuilder()
            .Ascii("II")
            .Bytes(0x2A, 0x00)
            .UInt32LE(firstIfdOffset);

        for (var page = 0; page < pageCount; page++)
        {
            builder.UInt16LE(entryCount);
            AddShortEntry(builder, 256, width);
            AddShortEntry(builder, 257, height);
            AddShortEntry(builder, 258, bitDepth);

            var isLast = page == pageCount - 1;
            var nextOffset = isLast ? 0u : (uint)(firstIfdOffset + (page + 1) * ifdSize);
            builder.UInt32LE(nextOffset);
        }

        return builder.ToArray();
    }

    public static byte[] TiffWithSelfReferencingIfd(int width, int height, int bitDepth)
    {
        const int entryCount = 3;
        const int firstIfdOffset = 8;

        var builder = new ByteBuilder()
            .Ascii("II")
            .Bytes(0x2A, 0x00)
            .UInt32LE(firstIfdOffset)
            .UInt16LE(entryCount);

        AddShortEntry(builder, 256, width);
        AddShortEntry(builder, 257, height);
        AddShortEntry(builder, 258, bitDepth);

        builder.UInt32LE(firstIfdOffset); // next-IFD offset points back at the same IFD

        return builder.ToArray();
    }

    private static void AddShortEntry(ByteBuilder builder, int tag, int value)
    {
        builder.UInt16LE(tag);
        builder.UInt16LE(3);
        builder.UInt32LE(1);
        builder.UInt16LE(value);
        builder.UInt16LE(0);
    }

    public static byte[] Psd(int width, int height, int depth = 8) =>
        new ByteBuilder()
            .Ascii("8BPS")
            .UInt16BE(1)
            .Zeros(6)
            .UInt16BE(3)
            .UInt32BE((uint)height)
            .UInt32BE((uint)width)
            .UInt16BE(depth)
            .UInt16BE(3)
            .ToArray();

    public static byte[] Emf(int width, int height) =>
        new ByteBuilder()
            .UInt32LE(1)
            .UInt32LE(88)
            .Int32LE(0)
            .Int32LE(0)
            .Int32LE(width)
            .Int32LE(height)
            .Zeros(16)
            .Bytes(0x20, 0x45, 0x4D, 0x46)
            .ToArray();

    public static byte[] Wmf(int width, int height)
    {
        const int inch = 1440;
        var right = width * inch / 96;
        var bottom = height * inch / 96;

        return new ByteBuilder()
            .Bytes(0x9A, 0xC6, 0xCD, 0xD7)
            .UInt16LE(0)
            .UInt16LE(0)
            .UInt16LE(0)
            .UInt16LE(right)
            .UInt16LE(bottom)
            .UInt16LE(inch)
            .UInt32LE(0)
            .UInt16LE(0)
            .ToArray();
    }

    public static byte[] Dicom() =>
        new ByteBuilder()
            .Zeros(128)
            .Ascii("DICM")
            .ToArray();

    public const string ImplicitVrLittleEndian = "1.2.840.10008.1.2";
    public const string ExplicitVrLittleEndian = "1.2.840.10008.1.2.1";
    public const string ExplicitVrBigEndian = "1.2.840.10008.1.2.2";

    public static byte[] DicomWithPixelInfo(
        int rows,
        int columns,
        int bitsAllocated,
        string transferSyntax = ExplicitVrLittleEndian)
    {
        var builder = new ByteBuilder().Zeros(128).Ascii("DICM");
        AppendTransferSyntax(builder, transferSyntax);

        if (transferSyntax == ImplicitVrLittleEndian)
        {
            AppendImplicitUShort(builder, 0x0028, 0x0010, rows);
            AppendImplicitUShort(builder, 0x0028, 0x0011, columns);
            AppendImplicitUShort(builder, 0x0028, 0x0100, bitsAllocated);
        }
        else if (transferSyntax == ExplicitVrBigEndian)
        {
            AppendExplicitUShortBigEndian(builder, 0x0028, 0x0010, rows);
            AppendExplicitUShortBigEndian(builder, 0x0028, 0x0011, columns);
            AppendExplicitUShortBigEndian(builder, 0x0028, 0x0100, bitsAllocated);
        }
        else
        {
            AppendExplicitUShort(builder, 0x0028, 0x0010, rows);
            AppendExplicitUShort(builder, 0x0028, 0x0011, columns);
            AppendExplicitUShort(builder, 0x0028, 0x0100, bitsAllocated);
        }

        return builder.ToArray();
    }

    private static void AppendTransferSyntax(ByteBuilder builder, string uid)
    {
        var padded = uid.Length % 2 == 0 ? uid : uid + "\0";
        builder.UInt16LE(0x0002).UInt16LE(0x0010).Ascii("UI").UInt16LE(padded.Length).Ascii(padded);
    }

    private static void AppendExplicitUShort(ByteBuilder builder, int group, int element, int value) =>
        builder.UInt16LE(group).UInt16LE(element).Ascii("US").UInt16LE(2).UInt16LE(value);

    private static void AppendExplicitUShortBigEndian(ByteBuilder builder, int group, int element, int value) =>
        builder.UInt16BE(group).UInt16BE(element).Ascii("US").UInt16BE(2).UInt16BE(value);

    private static void AppendImplicitUShort(ByteBuilder builder, int group, int element, int value) =>
        builder.UInt16LE(group).UInt16LE(element).UInt32LE(2).UInt16LE(value);

    public static byte[] DjVu(int width, int height) =>
        new ByteBuilder()
            .Ascii("AT&TFORM")
            .UInt32BE(16)       // form data: DJVU(4)+INFO(4)+size(4)+data(4)
            .Ascii("DJVU")
            .Ascii("INFO")
            .UInt32BE(4)        // INFO data size
            .UInt16BE(width)
            .UInt16BE(height)
            .ToArray();

    public static byte[] DjVuMultiPage(int width, int height, int pageCount) =>
        new ByteBuilder()
            .Ascii("AT&TFORM")
            .UInt32BE(40)       // form data: DJVM(4)+DIRM_chunk(12)+FORM_chunk(24)
            .Ascii("DJVM")
            // DIRM chunk: 3 bytes data (odd → 1 pad byte appended)
            .Ascii("DIRM")
            .UInt32BE(3)
            .Bytes(0x83)        // flags: bit7=bundled, version=3
            .UInt16BE(pageCount)
            .Bytes(0)           // IFF padding for odd-length chunk
            // First DJVU sub-form with INFO
            .Ascii("FORM")
            .UInt32BE(16)       // sub-form data: DJVU(4)+INFO(4)+size(4)+data(4)
            .Ascii("DJVU")
            .Ascii("INFO")
            .UInt32BE(4)
            .UInt16BE(width)
            .UInt16BE(height)
            .ToArray();

    public static byte[] Avif(int width, int height) =>
        new ByteBuilder()
            .UInt32BE(16).Ascii("ftyp").Ascii("avif").UInt32BE(0)
            .UInt32BE(48).Ascii("meta").UInt32BE(0)     // meta FullBox (version+flags=0)
            .UInt32BE(36).Ascii("iprp")
            .UInt32BE(28).Ascii("ipco")
            .UInt32BE(20).Ascii("ispe").UInt32BE(0)     // ispe FullBox
            .UInt32BE((uint)width)
            .UInt32BE((uint)height)
            .ToArray();

    public static byte[] Heic(int width, int height) =>
        new ByteBuilder()
            .UInt32BE(16).Ascii("ftyp").Ascii("heic").UInt32BE(0)
            .UInt32BE(48).Ascii("meta").UInt32BE(0)
            .UInt32BE(36).Ascii("iprp")
            .UInt32BE(28).Ascii("ipco")
            .UInt32BE(20).Ascii("ispe").UInt32BE(0)
            .UInt32BE((uint)width)
            .UInt32BE((uint)height)
            .ToArray();
}
