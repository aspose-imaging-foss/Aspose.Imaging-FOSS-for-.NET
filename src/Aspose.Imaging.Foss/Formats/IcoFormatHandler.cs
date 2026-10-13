using System.IO;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class IcoFormatHandler : IFormatHandler
{
    private static readonly byte[] Magic = { 0x00, 0x00, 0x01, 0x00 };

    public ImageFormat Format => ImageFormat.Ico;
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
        // ICONDIR(6) + the first ICONDIRENTRY up to and including its bit count(8).
        var buffer = new byte[14];
        stream.Seek(0, SeekOrigin.Begin);
        if (stream.ReadFully(buffer, 0, buffer.Length) < buffer.Length)
            return new ImageInfo(Format);

        var imageCount = EndianReader.ReadUInt16(buffer, 4, bigEndian: false);
        if (imageCount == 0)
            return new ImageInfo(Format);

        // A dimension byte of 0 means 256px, per the ICO directory entry spec.
        var width = buffer[6] == 0 ? 256 : buffer[6];
        var height = buffer[7] == 0 ? 256 : buffer[7];
        // Writers commonly leave this 0 and let the embedded image carry it.
        var bitCount = HeaderValues.PositiveOrNull(EndianReader.ReadUInt16(buffer, 12, bigEndian: false));

        return new ImageInfo(Format, width, height, bitCount, frameCount: imageCount);
    }
}
