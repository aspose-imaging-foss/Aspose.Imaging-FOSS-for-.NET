using System.IO;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class JpegFormatHandler : IFormatHandler
{
    private static readonly byte[] Magic = { 0xFF, 0xD8, 0xFF };

    public ImageFormat Format => ImageFormat.Jpeg;
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

    public ImageInfo ReadInfo(Stream stream)
    {
        stream.Seek(2, SeekOrigin.Begin);

        while (true)
        {
            var marker = ReadNextMarkerCode(stream);

            if (marker == 0xD8 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD9))
                continue;

            var lengthBuf = new byte[2];
            if (stream.ReadFully(lengthBuf, 0, 2) < 2)
                break;

            // The length field counts itself, so anything below 2 is malformed; skipping by it
            // would seek backwards and re-read the same marker forever.
            var segmentLength = EndianReader.ReadUInt16(lengthBuf, 0, bigEndian: true);
            if (segmentLength < 2)
                break;

            if (IsStartOfFrameMarker(marker))
            {
                var sof = new byte[5];
                stream.ReadFully(sof, 0, 5);
                var precision = sof[0];
                var height = EndianReader.ReadUInt16(sof, 1, bigEndian: true);
                var width = EndianReader.ReadUInt16(sof, 3, bigEndian: true);
                return new ImageInfo(Format, width, height, precision, frameCount: 1);
            }

            if (marker == 0xDA)
                break;

            stream.Seek(segmentLength - 2, SeekOrigin.Current);
        }

        return new ImageInfo(Format);
    }

    // SOF markers mark the start of frame data; C4/C8/CC are DHT/JPG-ext/DAC and share the 0xC0-0xCF range without carrying dimensions.
    private static bool IsStartOfFrameMarker(byte marker) =>
        marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;

    private static byte ReadNextMarkerCode(Stream stream)
    {
        int b;
        while ((b = stream.ReadByte()) != 0xFF)
        {
            if (b == -1)
                throw new EndOfStreamException();
        }

        while ((b = stream.ReadByte()) == 0xFF)
        {
        }

        if (b == -1)
            throw new EndOfStreamException();

        return (byte)b;
    }
}
