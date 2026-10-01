using System;
using System.IO;
using System.Text;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class DjVuFormatHandler : IFormatHandler
{
    public ImageFormat Format => ImageFormat.DjVu;
    public int SignatureLength => 8;

    public bool MatchesSignature(byte[] header, int headerLength) =>
        headerLength >= 8
        && header[0] == 'A' && header[1] == 'T' && header[2] == '&' && header[3] == 'T'
        && header[4] == 'F' && header[5] == 'O' && header[6] == 'R' && header[7] == 'M';

    public ImageInfo ReadInfo(Stream stream)
    {
        // AT&TFORM(8) + form data size(4 BE) + form type(4) = 16-byte header
        var header = new byte[16];
        stream.Seek(0, SeekOrigin.Begin);
        if (stream.ReadFully(header, 0, 16) < 16)
            return new ImageInfo(Format);

        var formDataSize = EndianReader.ReadUInt32(header, 8, bigEndian: true);
        var formEnd = 16L + formDataSize;
        var formType = Encoding.ASCII.GetString(header, 12, 4);

        if (formType == "DJVU")
        {
            var (w, h) = ReadInfoChunk(stream, 16, formEnd);
            return new ImageInfo(Format, w, h, frameCount: 1);
        }

        if (formType == "DJVM")
        {
            int? frameCount = null;
            int? width = null;
            int? height = null;

            WalkChunks(stream, 16, formEnd, (id, dataStart, dataEnd) =>
            {
                if (id == "DIRM" && frameCount == null)
                {
                    frameCount = ReadDirmFrameCount(stream, dataStart, dataEnd);
                }
                else if (id == "FORM" && width == null)
                {
                    var subTypeBuf = new byte[4];
                    stream.Seek(dataStart, SeekOrigin.Begin);
                    if (stream.ReadFully(subTypeBuf, 0, 4) >= 4
                        && Encoding.ASCII.GetString(subTypeBuf, 0, 4) == "DJVU")
                    {
                        var (w, h) = ReadInfoChunk(stream, dataStart + 4, dataEnd);
                        width = w;
                        height = h;
                    }
                }

                return width != null && frameCount != null;
            });

            return new ImageInfo(Format, width, height, frameCount: frameCount);
        }

        return new ImageInfo(Format);
    }

    private static (int? width, int? height) ReadInfoChunk(Stream stream, long regionStart, long regionEnd)
    {
        int? width = null;
        int? height = null;

        WalkChunks(stream, regionStart, regionEnd, (id, dataStart, dataEnd) =>
        {
            if (id != "INFO" || dataEnd - dataStart < 4)
                return false;

            var buf = new byte[4];
            stream.Seek(dataStart, SeekOrigin.Begin);
            if (stream.ReadFully(buf, 0, 4) >= 4)
            {
                width = (buf[0] << 8) | buf[1];
                height = (buf[2] << 8) | buf[3];
            }

            return true;
        });

        return (width, height);
    }

    private static int? ReadDirmFrameCount(Stream stream, long dataStart, long dataEnd)
    {
        if (dataEnd - dataStart < 3)
            return null;

        var buf = new byte[3];
        stream.Seek(dataStart, SeekOrigin.Begin);
        if (stream.ReadFully(buf, 0, 3) < 3)
            return null;

        return (buf[0] & 0x80) != 0 ? (int?)((buf[1] << 8) | buf[2]) : null;
    }

    // Walks IFF-style chunks in [regionStart, regionEnd). Callback receives (chunkId, dataStart, dataEnd)
    // and returns true to stop early.
    private static void WalkChunks(Stream stream, long regionStart, long regionEnd,
        Func<string, long, long, bool> callback)
    {
        var headerBuf = new byte[8];
        stream.Seek(regionStart, SeekOrigin.Begin);

        while (stream.Position + 8 <= regionEnd)
        {
            if (stream.ReadFully(headerBuf, 0, 8) < 8)
                break;

            var chunkId = Encoding.ASCII.GetString(headerBuf, 0, 4);
            var chunkDataSize = EndianReader.ReadUInt32(headerBuf, 4, bigEndian: true);
            var dataStart = stream.Position;
            var dataEnd = dataStart + (long)chunkDataSize;

            if (callback(chunkId, dataStart, dataEnd))
                break;

            // IFF chunks are padded to even length; size field stores actual (possibly odd) size.
            var nextPos = dataStart + (long)chunkDataSize + (chunkDataSize % 2 != 0 ? 1L : 0L);
            if (nextPos <= dataStart)
                break;

            stream.Seek(nextPos, SeekOrigin.Begin);
        }
    }
}
