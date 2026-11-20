using System.IO;
using System.Text;
using System.Threading.Tasks;
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

    public async Task<ImageInfo> ReadInfoAsync(ProbeReader stream)
    {
        // Signature(8) + IHDR length(4) + "IHDR"(4) + width(4) + height(4) + bit depth(1).
        var buffer = new byte[25];
        stream.Seek(0, SeekOrigin.Begin);
        if (await stream.ReadFullyAsync(buffer, 0, buffer.Length).ConfigureAwait(false) < buffer.Length)
            return new ImageInfo(Format);

        // IHDR must be the first chunk; anything else means the bytes at 16..24 aren't dimensions.
        if (Encoding.ASCII.GetString(buffer, 12, 4) != "IHDR" ||
            EndianReader.ReadUInt32(buffer, 8, bigEndian: true) != 13)
            return new ImageInfo(Format);

        var width = HeaderValues.PositiveOrNull(EndianReader.ReadUInt32(buffer, 16, bigEndian: true));
        var height = HeaderValues.PositiveOrNull(EndianReader.ReadUInt32(buffer, 20, bigEndian: true));
        var bitDepth = HeaderValues.PositiveOrNull(buffer[24]);

        var frameCount = await ReadFrameCountAsync(stream).ConfigureAwait(false);
        return new ImageInfo(Format, width, height, bitDepth, frameCount);
    }

    private static async Task<int?> ReadFrameCountAsync(ProbeReader stream)
    {
        // Skip the remaining four IHDR fields and the CRC. acTL must precede the first IDAT.
        stream.Skip(8);
        var chunk = new byte[8];
        while (await stream.ReadFullyAsync(chunk, 0, chunk.Length).ConfigureAwait(false) == chunk.Length)
        {
            var type = Encoding.ASCII.GetString(chunk, 4, 4);
            if (type == "IDAT")
                return 1;

            var dataLength = EndianReader.ReadUInt32(chunk, 0, bigEndian: true);
            var dataAndCrc = (long)dataLength + 4;
            if (type == "IEND" || dataAndCrc > stream.Length - stream.Position)
                return null;

            if (type == "acTL")
            {
                // num_frames and num_plays are separate 32-bit fields.
                if (dataLength != 8 || await stream.ReadFullyAsync(chunk, 0, 8).ConfigureAwait(false) != 8)
                    return null;
                return HeaderValues.PositiveOrNull(EndianReader.ReadUInt32(chunk, 0, bigEndian: true));
            }

            stream.Skip(dataAndCrc);
        }

        return null;
    }
}
