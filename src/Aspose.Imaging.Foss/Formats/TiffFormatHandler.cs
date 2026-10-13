using System.Collections.Generic;
using System.IO;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class TiffFormatHandler : IFormatHandler
{
    // Sanity cap on the number of IFDs walked, so a malformed/adversarial IFD chain
    // (e.g. one that keeps pointing forward into fabricated offsets) can't turn a
    // probe into an unbounded loop.
    private const int MaxIfds = 65536;

    public ImageFormat Format => ImageFormat.Tiff;
    public int SignatureLength => 4;

    public bool MatchesSignature(byte[] header, int headerLength)
    {
        var littleEndian = header[0] == 'I' && header[1] == 'I' && header[2] == 0x2A && header[3] == 0x00;
        var bigEndian = header[0] == 'M' && header[1] == 'M' && header[2] == 0x00 && header[3] == 0x2A;
        return littleEndian || bigEndian;
    }

    public ImageInfo ReadInfo(Stream stream)
    {
        var header = new byte[8];
        stream.Seek(0, SeekOrigin.Begin);
        stream.ReadFully(header, 0, header.Length);

        var bigEndian = header[0] == 'M';
        var ifdOffset = EndianReader.ReadUInt32(header, 4, bigEndian);

        int? width = null;
        int? height = null;
        int? bitDepth = null;
        var frameCount = 0;

        var visitedOffsets = new HashSet<uint>();
        var countBuf = new byte[2];
        var entry = new byte[12];
        var nextOffsetBuf = new byte[4];

        while (ifdOffset != 0 && visitedOffsets.Add(ifdOffset) && frameCount < MaxIfds)
        {
            stream.Seek(ifdOffset, SeekOrigin.Begin);
            if (stream.ReadFully(countBuf, 0, 2) < 2)
                break;

            var entryCount = EndianReader.ReadUInt16(countBuf, 0, bigEndian);

            // Only the first IFD's entries are parsed; later IFDs are just counted, so their
            // entries are skipped with a single seek. Reading them entry by entry would let a
            // chain of maximal IFDs cost billions of reads.
            if (frameCount > 0)
            {
                frameCount++;
                stream.Seek(entryCount * 12L, SeekOrigin.Current);
                if (stream.ReadFully(nextOffsetBuf, 0, 4) < 4)
                    break;

                ifdOffset = EndianReader.ReadUInt32(nextOffsetBuf, 0, bigEndian);
                continue;
            }

            var readOk = true;
            for (var i = 0; i < entryCount; i++)
            {
                if (stream.ReadFully(entry, 0, entry.Length) < entry.Length)
                {
                    readOk = false;
                    break;
                }

                var tag = EndianReader.ReadUInt16(entry, 0, bigEndian);
                var type = EndianReader.ReadUInt16(entry, 2, bigEndian);

                switch (tag)
                {
                    case 256:
                        width = ReadIfdValue(entry, type, bigEndian);
                        break;
                    case 257:
                        height = ReadIfdValue(entry, type, bigEndian);
                        break;
                    case 258:
                        bitDepth = ReadIfdValue(entry, type, bigEndian);
                        break;
                }
            }

            frameCount++;

            if (!readOk || stream.ReadFully(nextOffsetBuf, 0, 4) < 4)
                break;

            ifdOffset = EndianReader.ReadUInt32(nextOffsetBuf, 0, bigEndian);
        }

        return new ImageInfo(Format, width, height, bitDepth, frameCount == 0 ? null : frameCount);
    }

    // For SHORT/LONG types with a count of 1, TIFF stores the value left-justified in the entry's 4-byte value field.
    private static int ReadIfdValue(byte[] entry, ushort type, bool bigEndian) =>
        type == 3
            ? EndianReader.ReadUInt16(entry, 8, bigEndian)
            : EndianReader.ReadInt32(entry, 8, bigEndian);
}
