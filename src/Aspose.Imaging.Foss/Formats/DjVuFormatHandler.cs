using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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

    public async Task<ImageInfo> ReadInfoAsync(ProbeReader stream)
    {
        // AT&TFORM(8) + form data size(4 BE) + form type(4) = 16-byte header
        var header = new byte[16];
        stream.Seek(0, SeekOrigin.Begin);
        if (await stream.ReadFullyAsync(header, 0, 16).ConfigureAwait(false) < 16)
            return new ImageInfo(Format);

        // The FORM size counts everything after the size field, including the 4-byte form type.
        var formDataSize = EndianReader.ReadUInt32(header, 8, bigEndian: true);
        var formEnd = 12L + formDataSize;
        var formType = Encoding.ASCII.GetString(header, 12, 4);

        if (formType == "DJVU")
        {
            var (w, h) = await ReadInfoChunkAsync(stream, 16, formEnd).ConfigureAwait(false);
            return new ImageInfo(Format, w, h, frameCount: 1);
        }

        if (formType == "DJVM")
        {
            // DIRM's file count includes shared dictionaries (DJVI) and thumbnails (THUM), so it
            // overstates pages. A bundled document instead carries every component inline, and
            // each page is exactly one FORM:DJVU. An indirect document keeps its pages in separate
            // files, so its page count can't be known from this file alone.
            var bundled = false;
            var pageCount = 0;
            int? width = null;
            int? height = null;

            await WalkChunksAsync(stream, 16, formEnd, async (id, dataStart, dataEnd) =>
            {
                if (id == "DIRM")
                {
                    bundled = await IsBundledDirectoryAsync(stream, dataStart, dataEnd).ConfigureAwait(false);
                }
                else if (id == "FORM")
                {
                    var subTypeBuf = new byte[4];
                    stream.Seek(dataStart, SeekOrigin.Begin);
                    if (await stream.ReadFullyAsync(subTypeBuf, 0, 4).ConfigureAwait(false) >= 4
                        && Encoding.ASCII.GetString(subTypeBuf, 0, 4) == "DJVU")
                    {
                        pageCount++;
                        if (width == null)
                            (width, height) = await ReadInfoChunkAsync(stream, dataStart + 4, dataEnd).ConfigureAwait(false);
                    }
                }

                return false;
            }).ConfigureAwait(false);

            // A truncated file hides the components past its end, so a count would be a guess.
            var complete = formEnd <= stream.Length;
            int? frameCount = bundled && complete && pageCount > 0 ? pageCount : null;

            return new ImageInfo(Format, width, height, frameCount: frameCount);
        }

        return new ImageInfo(Format);
    }

    private static async Task<(int? width, int? height)> ReadInfoChunkAsync(ProbeReader stream, long regionStart, long regionEnd)
    {
        int? width = null;
        int? height = null;

        await WalkChunksAsync(stream, regionStart, regionEnd, async (id, dataStart, dataEnd) =>
        {
            if (id != "INFO" || dataEnd - dataStart < 4)
                return false;

            var buf = new byte[4];
            stream.Seek(dataStart, SeekOrigin.Begin);
            if (await stream.ReadFullyAsync(buf, 0, 4).ConfigureAwait(false) >= 4)
            {
                width = HeaderValues.PositiveOrNull((buf[0] << 8) | buf[1]);
                height = HeaderValues.PositiveOrNull((buf[2] << 8) | buf[3]);
            }

            return true;
        }).ConfigureAwait(false);

        return (width, height);
    }

    // DIRM starts with a flags byte whose top bit marks a bundled (single-file) document.
    private static async Task<bool> IsBundledDirectoryAsync(ProbeReader stream, long dataStart, long dataEnd)
    {
        if (dataEnd - dataStart < 1)
            return false;

        stream.Seek(dataStart, SeekOrigin.Begin);
        var flags = await stream.ReadByteAsync().ConfigureAwait(false);
        return flags != -1 && (flags & 0x80) != 0;
    }

    // Walks IFF-style chunks in [regionStart, regionEnd). Callback receives (chunkId, dataStart, dataEnd)
    // and returns true to stop early.
    private static async Task WalkChunksAsync(ProbeReader stream, long regionStart, long regionEnd,
        Func<string, long, long, Task<bool>> callback)
    {
        var headerBuf = new byte[8];
        stream.Seek(regionStart, SeekOrigin.Begin);

        while (stream.Position + 8 <= regionEnd)
        {
            if (await stream.ReadFullyAsync(headerBuf, 0, 8).ConfigureAwait(false) < 8)
                break;

            var chunkId = Encoding.ASCII.GetString(headerBuf, 0, 4);
            var chunkDataSize = EndianReader.ReadUInt32(headerBuf, 4, bigEndian: true);
            var dataStart = stream.Position;
            var dataEnd = dataStart + (long)chunkDataSize;

            if (await callback(chunkId, dataStart, dataEnd).ConfigureAwait(false))
                break;

            // IFF chunks are padded to even length; size field stores actual (possibly odd) size.
            var nextPos = dataStart + (long)chunkDataSize + (chunkDataSize % 2 != 0 ? 1L : 0L);
            if (nextPos <= dataStart)
                break;

            stream.SeekToOffset(nextPos);
        }
    }
}
