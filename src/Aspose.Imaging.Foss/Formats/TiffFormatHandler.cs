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

    private const ushort ShortType = 3;
    private const ushort LongType = 4;

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

                switch (tag)
                {
                    case 256:
                        width = HeaderValues.PositiveOrNull(ReadFirstValue(stream, entry, bigEndian));
                        break;
                    case 257:
                        height = HeaderValues.PositiveOrNull(ReadFirstValue(stream, entry, bigEndian));
                        break;
                    case 258:
                        // One value per sample (e.g. 8,8,8 for RGB); the first is reported, as per-channel depth.
                        bitDepth = HeaderValues.PositiveOrNull(ReadFirstValue(stream, entry, bigEndian));
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

    // Reads the first value of a SHORT or LONG entry. Values totalling 4 bytes or less are stored
    // left-justified in the entry's value field; larger arrays live at the offset that field holds.
    // Any other type is invalid for the tags read here. The stream position is preserved.
    private static uint? ReadFirstValue(Stream stream, byte[] entry, bool bigEndian)
    {
        var type = EndianReader.ReadUInt16(entry, 2, bigEndian);
        var count = EndianReader.ReadUInt32(entry, 4, bigEndian);

        var size = type switch
        {
            ShortType => 2,
            LongType => 4,
            _ => 0,
        };

        if (size == 0 || count == 0)
            return null;

        if ((ulong)count * (ulong)size <= 4)
            return ReadValue(entry, 8, size, bigEndian);

        var valueOffset = EndianReader.ReadUInt32(entry, 8, bigEndian);
        var resumePosition = stream.Position;

        var valueBuf = new byte[4];
        stream.Seek(valueOffset, SeekOrigin.Begin);
        var read = stream.ReadFully(valueBuf, 0, size);
        stream.Seek(resumePosition, SeekOrigin.Begin);

        return read < size ? null : ReadValue(valueBuf, 0, size, bigEndian);
    }

    private static uint ReadValue(byte[] buffer, int offset, int size, bool bigEndian) =>
        size == 2
            ? EndianReader.ReadUInt16(buffer, offset, bigEndian)
            : EndianReader.ReadUInt32(buffer, offset, bigEndian);
}
