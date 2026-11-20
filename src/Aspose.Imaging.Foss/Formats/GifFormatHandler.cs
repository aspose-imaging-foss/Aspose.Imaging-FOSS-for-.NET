using System.IO;
using System.Text;
using System.Threading.Tasks;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class GifFormatHandler : IFormatHandler
{
    private const int ExtensionIntroducer = 0x21;
    private const int ImageDescriptor = 0x2C;
    private const int Trailer = 0x3B;

    public ImageFormat Format => ImageFormat.Gif;
    public int SignatureLength => 6;

    public bool MatchesSignature(byte[] header, int headerLength)
    {
        var signature = Encoding.ASCII.GetString(header, 0, 6);
        return signature == "GIF87a" || signature == "GIF89a";
    }

    public async Task<ImageInfo> ReadInfoAsync(ProbeReader stream)
    {
        stream.Seek(6, SeekOrigin.Begin);
        var screenDescriptor = new byte[7];
        if (await stream.ReadFullyAsync(screenDescriptor, 0, screenDescriptor.Length).ConfigureAwait(false) < screenDescriptor.Length)
            return new ImageInfo(Format);

        var width = EndianReader.ReadUInt16(screenDescriptor, 0, bigEndian: false);
        var height = EndianReader.ReadUInt16(screenDescriptor, 2, bigEndian: false);

        SkipColorTableIfPresent(stream, screenDescriptor[4]);

        var frameCount = await CountFramesAsync(stream).ConfigureAwait(false);

        return new ImageInfo(Format,
            HeaderValues.PositiveOrNull(width), HeaderValues.PositiveOrNull(height), frameCount: frameCount);
    }

    private static async Task<int?> CountFramesAsync(ProbeReader stream)
    {
        var frameCount = 0;

        while (true)
        {
            var blockType = await stream.ReadByteAsync().ConfigureAwait(false);
            if (blockType == -1 || blockType == Trailer)
                break;

            if (blockType == ExtensionIntroducer)
            {
                await stream.ReadByteAsync().ConfigureAwait(false); // extension label
                if (!await SkipSubBlocksAsync(stream).ConfigureAwait(false))
                    break;
            }
            else if (blockType == ImageDescriptor)
            {
                var descriptor = new byte[9];
                if (await stream.ReadFullyAsync(descriptor, 0, descriptor.Length).ConfigureAwait(false) < descriptor.Length)
                    break;

                frameCount++;

                SkipColorTableIfPresent(stream, descriptor[8]);

                await stream.ReadByteAsync().ConfigureAwait(false); // LZW minimum code size
                if (!await SkipSubBlocksAsync(stream).ConfigureAwait(false))
                    break;
            }
            else
            {
                // Unrecognized block: the stream can't be reliably walked any further.
                break;
            }
        }

        return frameCount == 0 ? null : frameCount;
    }

    private static void SkipColorTableIfPresent(ProbeReader stream, byte packedFields)
    {
        if ((packedFields & 0x80) == 0)
            return;

        var tableSize = 3 * (1 << ((packedFields & 0x07) + 1));
        stream.Skip(tableSize);
    }

    // Sub-blocks are a size byte followed by that many data bytes, terminated by a zero-size block.
    private static async Task<bool> SkipSubBlocksAsync(ProbeReader stream)
    {
        while (true)
        {
            var size = await stream.ReadByteAsync().ConfigureAwait(false);
            if (size <= 0)
                return size == 0;

            stream.Skip(size);
        }
    }
}
