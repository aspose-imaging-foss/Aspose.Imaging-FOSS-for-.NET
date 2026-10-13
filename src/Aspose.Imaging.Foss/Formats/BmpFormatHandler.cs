using System;
using System.IO;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class BmpFormatHandler : IFormatHandler
{
    // BITMAPCOREHEADER (OS/2 1.x): 16-bit unsigned width/height, then planes and bit count.
    private const uint CoreHeaderSize = 12;

    public ImageFormat Format => ImageFormat.Bmp;

    // "BM" + file size(4) + reserved(4) + pixel data offset(4) + DIB header size(4).
    public int SignatureLength => 18;

    // "BM" alone is too weak (any text starting with it would match), so the DIB header size
    // must also be one that a real BMP uses.
    public bool MatchesSignature(byte[] header, int headerLength) =>
        header[0] == (byte)'B' && header[1] == (byte)'M' &&
        IsKnownDibHeaderSize(EndianReader.ReadUInt32(header, 14, bigEndian: false));

    public ImageInfo ReadInfo(Stream stream)
    {
        var buffer = new byte[30];
        stream.Seek(0, SeekOrigin.Begin);
        var read = stream.ReadFully(buffer, 0, buffer.Length);

        var dibHeaderSize = EndianReader.ReadUInt32(buffer, 14, bigEndian: false);

        if (dibHeaderSize == CoreHeaderSize)
        {
            if (read < 26)
                return new ImageInfo(Format);

            var coreWidth = EndianReader.ReadUInt16(buffer, 18, bigEndian: false);
            var coreHeight = EndianReader.ReadUInt16(buffer, 20, bigEndian: false);
            var coreBitCount = EndianReader.ReadUInt16(buffer, 24, bigEndian: false);

            return new ImageInfo(Format,
                HeaderValues.PositiveOrNull(coreWidth),
                HeaderValues.PositiveOrNull(coreHeight),
                HeaderValues.PositiveOrNull(coreBitCount),
                frameCount: 1);
        }

        // BITMAPINFOHEADER and its successors (V4/V5, OS/2 2.x) share this leading layout.
        if (read < 30)
            return new ImageInfo(Format);

        var width = EndianReader.ReadInt32(buffer, 18, bigEndian: false);
        // BMP stores a negative height for top-down (non-flipped) pixel layouts; magnitude is the pixel height.
        var height = Math.Abs((long)EndianReader.ReadInt32(buffer, 22, bigEndian: false));
        // 0 means the bit count is implied by an embedded JPEG/PNG stream.
        var bitCount = EndianReader.ReadUInt16(buffer, 28, bigEndian: false);

        return new ImageInfo(Format,
            HeaderValues.PositiveOrNull(width),
            HeaderValues.PositiveOrNull(height),
            HeaderValues.PositiveOrNull(bitCount),
            frameCount: 1);
    }

    // 12: OS/2 1.x core; 16-64: OS/2 2.x (variable length) and Windows 40/52/56; 108/124: V4/V5.
    private static bool IsKnownDibHeaderSize(uint size) =>
        size == CoreHeaderSize || (size >= 16 && size <= 64) || size == 108 || size == 124;
}
