using System.IO;
using System.Text;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class PsdFormatHandler : IFormatHandler
{
    public ImageFormat Format => ImageFormat.Psd;
    public int SignatureLength => 4;

    public bool MatchesSignature(byte[] header, int headerLength) =>
        Encoding.ASCII.GetString(header, 0, 4) == "8BPS";

    public ImageInfo ReadInfo(Stream stream)
    {
        // Signature(4) + version(2) + reserved(6) + channels(2) + height(4) + width(4) + depth(2).
        var buffer = new byte[24];
        stream.Seek(0, SeekOrigin.Begin);
        if (stream.ReadFully(buffer, 0, buffer.Length) < buffer.Length)
            return new ImageInfo(Format);

        var height = HeaderValues.PositiveOrNull(EndianReader.ReadUInt32(buffer, 14, bigEndian: true));
        var width = HeaderValues.PositiveOrNull(EndianReader.ReadUInt32(buffer, 18, bigEndian: true));
        var depth = HeaderValues.PositiveOrNull(EndianReader.ReadUInt16(buffer, 22, bigEndian: true));

        return new ImageInfo(Format, width, height, depth, frameCount: 1);
    }
}
