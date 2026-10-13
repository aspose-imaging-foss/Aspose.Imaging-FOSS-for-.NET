using System;

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

    // Ends right after a marker code, so the segment length can't be read (found by fuzzing).
    public static byte[] JpegEndingAfterMarker() =>
        new byte[] { 0xFF, 0xD8, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0A };

    // A segment whose length field is below its own 2-byte size.
    public static byte[] JpegWithZeroSegmentLength() =>
        new ByteBuilder()
            .Bytes(0xFF, 0xD8, 0xFF, 0xE0)
            .UInt16BE(0)
            .Bytes(0xFF, 0xE1)
            .UInt16BE(0)
            .ToArray();

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

    // OS/2 1.x bitmap: 12-byte BITMAPCOREHEADER with 16-bit unsigned width/height.
    public static byte[] BmpCore(int width, int height, int bitCount = 24) =>
        new ByteBuilder()
            .Ascii("BM")
            .UInt32LE(0)
            .UInt32LE(0)
            .UInt32LE(26)
            .UInt32LE(12)
            .UInt16LE(width)
            .UInt16LE(height)
            .UInt16LE(1)
            .UInt16LE(bitCount)
            .ToArray();

    // BITMAPINFOHEADER-style layout with an arbitrary declared DIB header size (V4 = 108, V5 = 124, ...).
    public static byte[] BmpWithHeaderSize(uint headerSize, int width, int height, int bitCount = 24) =>
        new ByteBuilder()
            .Ascii("BM")
            .UInt32LE(0)
            .UInt32LE(0)
            .UInt32LE(14 + headerSize)
            .UInt32LE(headerSize)
            .Int32LE(width)
            .Int32LE(height)
            .UInt16LE(1)
            .UInt16LE(bitCount)
            .Zeros((int)headerSize - 16)
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

    // IFDs packed 4 bytes apart, each claiming the maximum 65535 entries; their next-IFD
    // pointers sit past the (overlapping) entry tables and chain to the following IFD.
    public static byte[] TiffWithMaximalIfdChain(int ifdCount)
    {
        const int entryTableEnd = 2 + 65535 * 12;
        var data = new byte[8 + 4 * ifdCount + entryTableEnd + 4];

        data[0] = (byte)'I';
        data[1] = (byte)'I';
        data[2] = 0x2A;
        WriteUInt32LE(data, 4, 8);

        for (var i = 0; i < ifdCount; i++)
        {
            var offset = 8 + 4 * i;
            data[offset] = 0xFF;
            data[offset + 1] = 0xFF;
            var next = i == ifdCount - 1 ? 0u : (uint)(offset + 4);
            WriteUInt32LE(data, offset + entryTableEnd, next);
        }

        return data;
    }

    private static void WriteUInt32LE(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }

    // Little-endian TIFF whose single IFD holds the given raw entries (tag, type, count, value
    // field), followed by `trailer` at offset TiffTrailerOffset(entries.Length) for out-of-line values.
    public static byte[] TiffWithEntries((int tag, int type, uint count, uint value)[] entries, byte[]? trailer = null)
    {
        var builder = new ByteBuilder()
            .Ascii("II")
            .Bytes(0x2A, 0x00)
            .UInt32LE(8)
            .UInt16LE(entries.Length);

        foreach (var (tag, type, count, value) in entries)
            builder.UInt16LE(tag).UInt16LE(type).UInt32LE(count).UInt32LE(value);

        builder.UInt32LE(0); // no next IFD
        if (trailer is not null)
            builder.Bytes(trailer);

        return builder.ToArray();
    }

    public static uint TiffTrailerOffset(int entryCount) => (uint)(8 + 2 + entryCount * 12 + 4);

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

    // rclBounds is inclusive-inclusive, so a width x height image spans (0, 0)-(width-1, height-1).
    public static byte[] Emf(int width, int height) => EmfWithBounds(0, 0, width - 1, height - 1);

    public static byte[] EmfWithBounds(int left, int top, int right, int bottom) =>
        new ByteBuilder()
            .UInt32LE(1)
            .UInt32LE(88)
            .Int32LE(left)
            .Int32LE(top)
            .Int32LE(right)
            .Int32LE(bottom)
            .Zeros(16)
            .Bytes(0x20, 0x45, 0x4D, 0x46)
            .ToArray();

    public static byte[] Wmf(int width, int height)
    {
        const int inch = 1440;
        return WmfWithBounds(0, 0, width * inch / 96, height * inch / 96, inch);
    }

    public static byte[] WmfWithBounds(int left, int top, int right, int bottom, int inch) =>
        new ByteBuilder()
            .Bytes(0x9A, 0xC6, 0xCD, 0xD7)
            .UInt16LE(0)
            .UInt16LE(left)
            .UInt16LE(top)
            .UInt16LE(right)
            .UInt16LE(bottom)
            .UInt16LE(inch)
            .UInt32LE(0)
            .UInt16LE(0)
            .ToArray();

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

    // Transfer Syntax UID declared with a long-form VR and a ~2 GB length in a tiny file.
    public static byte[] DicomWithOversizedTransferSyntaxLength() =>
        new ByteBuilder()
            .Zeros(128)
            .Ascii("DICM")
            .UInt16LE(0x0002).UInt16LE(0x0010).Ascii("OB").UInt16LE(0).UInt32LE(0x7FFF0000)
            .Ascii(ImplicitVrLittleEndian)
            .ToArray();

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

    // A multi-page DJVM document laid out like real ones: a DIRM directory, then a shared
    // dictionary (FORM:DJVI) before the pages and a thumbnail form (FORM:THUM) after them.
    // DIRM's file count therefore exceeds the page count by two. Page i is (width + i) wide, so
    // tests can tell the first page's INFO apart from the others.
    public static byte[] DjVuMultiPage(int width, int height, int pageCount, bool bundled = true)
    {
        var fileCount = pageCount + 2;

        var directory = new ByteBuilder()
            .Bytes((byte)((bundled ? 0x80 : 0x00) | 1)) // flags: bit7 = bundled, version 1
            .UInt16BE(fileCount);
        if (bundled)
            directory.Zeros(4 * fileCount); // component offsets; not read by the probe

        var body = new ByteBuilder()
            .Ascii("DJVM")
            .Bytes(IffChunk("DIRM", directory.ToArray()));

        if (bundled)
        {
            body.Bytes(IffChunk("FORM", Concat(Ascii("DJVI"), IffChunk("Djbz", Array.Empty<byte>()))));

            for (var page = 0; page < pageCount; page++)
                body.Bytes(DjVuPageForm(width + page, height));

            body.Bytes(IffChunk("FORM", Concat(Ascii("THUM"), IffChunk("TH44", new byte[] { 0 }))));
        }

        return Concat(Ascii("AT&T"), IffChunk("FORM", body.ToArray()));
    }

    private static byte[] DjVuPageForm(int width, int height) =>
        IffChunk("FORM", Concat(
            Ascii("DJVU"),
            IffChunk("INFO", new ByteBuilder().UInt16BE(width).UInt16BE(height).ToArray())));

    // IFF chunk: id + big-endian data size + data, padded to an even length.
    private static byte[] IffChunk(string id, byte[] data)
    {
        var builder = new ByteBuilder().Ascii(id).UInt32BE((uint)data.Length).Bytes(data);
        if (data.Length % 2 != 0)
            builder.Bytes(0);
        return builder.ToArray();
    }

    // ISOBMFF HEIF/AVIF file with a configurable property layout. ipco holds a placeholder hvcC
    // at property index 1 followed by one ispe per entry of `ispes` (indices 2, 3, ...), so
    // tests also cover 1-based indexing past non-ispe properties. `associations` lists
    // (item ID, property indices) for ipma; pitm is written only when primaryItemId is set.
    public static byte[] Heif(
        string majorBrand,
        string[] compatibleBrands,
        (uint width, uint height)[] ispes,
        uint? primaryItemId = null,
        (uint itemId, int[] propertyIndices)[]? associations = null,
        int pitmVersion = 0,
        int ipmaVersion = 0,
        bool wideIpmaIndices = false)
    {
        var ftyp = new ByteBuilder().Ascii(majorBrand).UInt32BE(0);
        foreach (var brand in compatibleBrands)
            ftyp.Ascii(brand);

        var ipco = new ByteBuilder().Bytes(IsoBox("hvcC", new byte[4]));
        foreach (var (width, height) in ispes)
            ipco.Bytes(IsoBox("ispe", new ByteBuilder().UInt32BE(0).UInt32BE(width).UInt32BE(height).ToArray()));

        var iprp = new ByteBuilder().Bytes(IsoBox("ipco", ipco.ToArray()));
        if (associations is not null)
        {
            var ipma = new ByteBuilder()
                .Bytes((byte)ipmaVersion, 0, 0, (byte)(wideIpmaIndices ? 1 : 0))
                .UInt32BE((uint)associations.Length);

            foreach (var (itemId, propertyIndices) in associations)
            {
                if (ipmaVersion < 1)
                    ipma.UInt16BE((int)itemId);
                else
                    ipma.UInt32BE(itemId);

                ipma.Bytes((byte)propertyIndices.Length);
                foreach (var index in propertyIndices)
                {
                    // High bit = "essential" flag, which the probe must mask off.
                    if (wideIpmaIndices)
                        ipma.UInt16BE(0x8000 | index);
                    else
                        ipma.Bytes((byte)(0x80 | index));
                }
            }

            iprp.Bytes(IsoBox("ipma", ipma.ToArray()));
        }

        var meta = new ByteBuilder().UInt32BE(0); // FullBox version + flags
        if (primaryItemId is uint id)
        {
            var pitm = new ByteBuilder().Bytes((byte)pitmVersion, 0, 0, 0);
            if (pitmVersion == 0)
                pitm.UInt16BE((int)id);
            else
                pitm.UInt32BE(id);

            meta.Bytes(IsoBox("pitm", pitm.ToArray()));
        }

        meta.Bytes(IsoBox("iprp", iprp.ToArray()));

        return Concat(IsoBox("ftyp", ftyp.ToArray()), IsoBox("meta", meta.ToArray()));
    }

    private static byte[] IsoBox(string type, byte[] payload) =>
        new ByteBuilder().UInt32BE((uint)(8 + payload.Length)).Ascii(type).Bytes(payload).ToArray();

    private static byte[] Ascii(string value) => new ByteBuilder().Ascii(value).ToArray();

    private static byte[] Concat(params byte[][] parts)
    {
        var builder = new ByteBuilder();
        foreach (var part in parts)
            builder.Bytes(part);
        return builder.ToArray();
    }

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
