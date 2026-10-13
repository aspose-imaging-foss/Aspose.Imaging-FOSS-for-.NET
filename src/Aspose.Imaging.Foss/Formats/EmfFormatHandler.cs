using System.IO;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class EmfFormatHandler : IFormatHandler
{
    private static readonly byte[] SignatureMagic = { 0x20, 0x45, 0x4D, 0x46 };

    public ImageFormat Format => ImageFormat.Emf;
    public int SignatureLength => 44;

    public bool MatchesSignature(byte[] header, int headerLength)
    {
        var iType = EndianReader.ReadUInt32(header, 0, bigEndian: false);
        if (iType != 1)
            return false;

        for (var i = 0; i < SignatureMagic.Length; i++)
        {
            if (header[40 + i] != SignatureMagic[i])
                return false;
        }
        return true;
    }

    public ImageInfo ReadInfo(Stream stream)
    {
        var buffer = new byte[24];
        stream.Seek(0, SeekOrigin.Begin);
        stream.ReadFully(buffer, 0, buffer.Length);

        // EMF has no explicit width/height field; pixel dimensions are derived from the rclBounds device-space rectangle.
        long left = EndianReader.ReadInt32(buffer, 8, bigEndian: false);
        long top = EndianReader.ReadInt32(buffer, 12, bigEndian: false);
        long right = EndianReader.ReadInt32(buffer, 16, bigEndian: false);
        long bottom = EndianReader.ReadInt32(buffer, 20, bigEndian: false);

        // rclBounds is inclusive-inclusive (MS-EMF 2.2.9), so a 1-pixel image has left == right.
        // An empty metafile conventionally stores (0, 0, -1, -1), which yields unknown.
        return new ImageInfo(Format,
            HeaderValues.PositiveOrNull(right - left + 1),
            HeaderValues.PositiveOrNull(bottom - top + 1),
            frameCount: 1);
    }
}
