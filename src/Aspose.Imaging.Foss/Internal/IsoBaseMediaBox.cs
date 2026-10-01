using System.IO;
using System.Text;

namespace Aspose.Imaging.Foss.Internal;

// Helpers for ISO Base Media File Format (ISOBMFF) box walking, shared by AVIF and HEIC handlers.
internal static class IsoBaseMediaBox
{
    // Reads image dimensions from the ispe property box via meta → iprp → ipco → ispe.
    internal static (int? width, int? height) ReadImageSize(Stream stream)
    {
        long fileEnd = stream.Length;
        stream.Seek(0, SeekOrigin.Begin);

        if (!TryFind(stream, fileEnd, "meta", out var metaStart, out var metaEnd))
            return (null, null);

        // meta is a FullBox: version(1) + flags(3) precede nested boxes.
        stream.Seek(metaStart + 4, SeekOrigin.Begin);

        if (!TryFind(stream, metaEnd, "iprp", out _, out var iprpEnd))
            return (null, null);

        if (!TryFind(stream, iprpEnd, "ipco", out _, out var ipcoEnd))
            return (null, null);

        if (!TryFind(stream, ipcoEnd, "ispe", out var ispeStart, out _))
            return (null, null);

        // ispe is a FullBox: version(1) + flags(3) + image_width(4 BE) + image_height(4 BE).
        stream.Seek(ispeStart + 4, SeekOrigin.Begin);
        var buf = new byte[8];
        if (stream.ReadFully(buf, 0, 8) < 8)
            return (null, null);

        return (
            (int)EndianReader.ReadUInt32(buf, 0, bigEndian: true),
            (int)EndianReader.ReadUInt32(buf, 4, bigEndian: true)
        );
    }

    // Searches [stream.Position, regionEnd) for the first box with the given 4-char type.
    // On success: positions the stream at dataStart and sets dataStart/dataEnd.
    private static bool TryFind(Stream stream, long regionEnd, string targetType,
        out long dataStart, out long dataEnd)
    {
        dataStart = dataEnd = 0;
        var headerBuf = new byte[8];

        while (stream.Position + 8 <= regionEnd)
        {
            long boxStart = stream.Position;
            if (stream.ReadFully(headerBuf, 0, 8) < 8)
                break;

            uint sizeField = EndianReader.ReadUInt32(headerBuf, 0, bigEndian: true);
            string boxType = Encoding.ASCII.GetString(headerBuf, 4, 4);

            long thisDataStart, boxEnd;

            if (sizeField == 1)
            {
                // 64-bit extended size immediately follows the 8-byte header.
                var extBuf = new byte[8];
                if (stream.ReadFully(extBuf, 0, 8) < 8)
                    break;

                ulong extSize = ((ulong)extBuf[0] << 56) | ((ulong)extBuf[1] << 48)
                    | ((ulong)extBuf[2] << 40) | ((ulong)extBuf[3] << 32)
                    | ((ulong)extBuf[4] << 24) | ((ulong)extBuf[5] << 16)
                    | ((ulong)extBuf[6] << 8) | extBuf[7];

                if (extSize < 16 || extSize > (ulong)long.MaxValue)
                    break;

                thisDataStart = boxStart + 16;
                boxEnd = boxStart + (long)extSize;
            }
            else if (sizeField == 0)
            {
                thisDataStart = boxStart + 8;
                boxEnd = regionEnd;
            }
            else
            {
                if (sizeField < 8)
                    break;

                thisDataStart = boxStart + 8;
                boxEnd = boxStart + sizeField;
            }

            if (boxType == targetType)
            {
                dataStart = thisDataStart;
                dataEnd = boxEnd;
                stream.Seek(dataStart, SeekOrigin.Begin);
                return true;
            }

            if (boxEnd <= boxStart)
                break;

            stream.Seek(boxEnd, SeekOrigin.Begin);
        }

        return false;
    }
}
