using System.IO;
using System.Text;
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

    public ImageInfo ReadInfo(Stream stream)
    {
        stream.Seek(6, SeekOrigin.Begin);
        var screenDescriptor = new byte[7];
        if (stream.ReadFully(screenDescriptor, 0, screenDescriptor.Length) < screenDescriptor.Length)
            return new ImageInfo(Format);

        var width = EndianReader.ReadUInt16(screenDescriptor, 0, bigEndian: false);
        var height = EndianReader.ReadUInt16(screenDescriptor, 2, bigEndian: false);

        SkipColorTableIfPresent(stream, screenDescriptor[4]);

        var frameCount = CountFrames(stream);

        return new ImageInfo(Format,
            HeaderValues.PositiveOrNull(width), HeaderValues.PositiveOrNull(height), frameCount: frameCount);
    }

    private static int? CountFrames(Stream stream)
    {
        var frameCount = 0;

        while (true)
        {
            var blockType = stream.ReadByte();
            if (blockType == -1 || blockType == Trailer)
                break;

            if (blockType == ExtensionIntroducer)
            {
                stream.ReadByte(); // extension label
                if (!SkipSubBlocks(stream))
                    break;
            }
            else if (blockType == ImageDescriptor)
            {
                var descriptor = new byte[9];
                if (stream.ReadFully(descriptor, 0, descriptor.Length) < descriptor.Length)
                    break;

                frameCount++;

                SkipColorTableIfPresent(stream, descriptor[8]);

                stream.ReadByte(); // LZW minimum code size
                if (!SkipSubBlocks(stream))
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

    private static void SkipColorTableIfPresent(Stream stream, byte packedFields)
    {
        if ((packedFields & 0x80) == 0)
            return;

        var tableSize = 3 * (1 << ((packedFields & 0x07) + 1));
        stream.Seek(tableSize, SeekOrigin.Current);
    }

    // Sub-blocks are a size byte followed by that many data bytes, terminated by a zero-size block.
    private static bool SkipSubBlocks(Stream stream)
    {
        while (true)
        {
            var size = stream.ReadByte();
            if (size <= 0)
                return size == 0;

            stream.Seek(size, SeekOrigin.Current);
        }
    }
}
