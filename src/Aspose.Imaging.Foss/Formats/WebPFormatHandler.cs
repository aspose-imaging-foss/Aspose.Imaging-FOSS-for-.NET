using System.IO;
using System.Text;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class WebPFormatHandler : IFormatHandler
{
    public ImageFormat Format => ImageFormat.WebP;
    public int SignatureLength => 12;

    public bool MatchesSignature(byte[] header, int headerLength)
    {
        var riff = Encoding.ASCII.GetString(header, 0, 4);
        var webp = Encoding.ASCII.GetString(header, 8, 4);
        return riff == "RIFF" && webp == "WEBP";
    }

    public ImageInfo ReadInfo(Stream stream)
    {
        // RIFF header(12) + chunk header(8) + the longest bitstream header read below(10).
        var buffer = new byte[30];
        stream.Seek(0, SeekOrigin.Begin);
        var read = stream.ReadFully(buffer, 0, buffer.Length);
        if (read < 16)
            return new ImageInfo(Format);

        var fourCc = Encoding.ASCII.GetString(buffer, 12, 4);
        int width, height;

        switch (fourCc)
        {
            case "VP8 ":
                // Lossy: 3-byte frame tag + 3-byte start code (0x9d 0x01 0x2a), then 14-bit width/height (top 2 bits are scale factor).
                if (read < 30 || buffer[23] != 0x9D || buffer[24] != 0x01 || buffer[25] != 0x2A)
                    return new ImageInfo(Format);

                width = EndianReader.ReadUInt16(buffer, 26, bigEndian: false) & 0x3FFF;
                height = EndianReader.ReadUInt16(buffer, 28, bigEndian: false) & 0x3FFF;
                break;
            case "VP8L":
                // Lossless: 1-byte signature (0x2F) then a 4-byte little-endian field packing 14-bit (width-1) and 14-bit (height-1).
                if (read < 25 || buffer[20] != 0x2F)
                    return new ImageInfo(Format);

                var packed = EndianReader.ReadUInt32(buffer, 21, bigEndian: false);
                width = (int)(packed & 0x3FFF) + 1;
                height = (int)((packed >> 14) & 0x3FFF) + 1;
                break;
            case "VP8X":
                // Extended: 1-byte flags + 3-byte reserved, then 24-bit little-endian (canvasWidth-1) and (canvasHeight-1).
                if (read < 30)
                    return new ImageInfo(Format);

                width = EndianReader.ReadUInt24LittleEndian(buffer, 24) + 1;
                height = EndianReader.ReadUInt24LittleEndian(buffer, 27) + 1;
                break;
            default:
                return new ImageInfo(Format);
        }

        return new ImageInfo(Format, HeaderValues.PositiveOrNull(width), HeaderValues.PositiveOrNull(height), frameCount: 1);
    }
}
