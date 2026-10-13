using System.IO;
using System.Text;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class PngFormatHandler : IFormatHandler
{
    private static readonly byte[] Magic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public ImageFormat Format => ImageFormat.Png;
    public int SignatureLength => Magic.Length;

    public bool MatchesSignature(byte[] header, int headerLength)
    {
        for (var i = 0; i < Magic.Length; i++)
        {
            if (header[i] != Magic[i])
                return false;
        }
        return true;
    }

    public ImageInfo ReadInfo(Stream stream)
    {
        // Signature(8) + IHDR length(4) + "IHDR"(4) + width(4) + height(4) + bit depth(1).
        var buffer = new byte[25];
        stream.Seek(0, SeekOrigin.Begin);
        if (stream.ReadFully(buffer, 0, buffer.Length) < buffer.Length)
            return new ImageInfo(Format);

        // IHDR must be the first chunk; anything else means the bytes at 16..24 aren't dimensions.
        if (Encoding.ASCII.GetString(buffer, 12, 4) != "IHDR")
            return new ImageInfo(Format);

        var width = HeaderValues.PositiveOrNull(EndianReader.ReadUInt32(buffer, 16, bigEndian: true));
        var height = HeaderValues.PositiveOrNull(EndianReader.ReadUInt32(buffer, 20, bigEndian: true));
        var bitDepth = HeaderValues.PositiveOrNull(buffer[24]);

        return new ImageInfo(Format, width, height, bitDepth, frameCount: 1);
    }
}
