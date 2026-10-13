using System;
using System.IO;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class WmfFormatHandler : IFormatHandler
{
    private static readonly byte[] PlaceableMagic = { 0x9A, 0xC6, 0xCD, 0xD7 };

    public ImageFormat Format => ImageFormat.Wmf;
    public int SignatureLength => 6;

    public bool MatchesSignature(byte[] header, int headerLength)
    {
        if (Matches(header, PlaceableMagic))
            return true;

        // Standard (non-placeable) WMF has no magic number; a plausible mfType/headerSize/version combo is the best signal available.
        var type = EndianReader.ReadUInt16(header, 0, bigEndian: false);
        var headerSize = EndianReader.ReadUInt16(header, 2, bigEndian: false);
        var version = EndianReader.ReadUInt16(header, 4, bigEndian: false);

        return (type == 1 || type == 2) && headerSize == 9 && (version == 0x0100 || version == 0x0300);
    }

    public ImageInfo ReadInfo(Stream stream)
    {
        // Placeable header up to and including Inch: key(4) + hWmf(2) + bounding box(8) + inch(2).
        var buffer = new byte[16];
        stream.Seek(0, SeekOrigin.Begin);
        if (stream.ReadFully(buffer, 0, buffer.Length) < buffer.Length)
            return new ImageInfo(Format);

        if (!Matches(buffer, PlaceableMagic))
            return new ImageInfo(Format);

        var left = (short)EndianReader.ReadUInt16(buffer, 6, bigEndian: false);
        var top = (short)EndianReader.ReadUInt16(buffer, 8, bigEndian: false);
        var right = (short)EndianReader.ReadUInt16(buffer, 10, bigEndian: false);
        var bottom = (short)EndianReader.ReadUInt16(buffer, 12, bigEndian: false);
        var inch = EndianReader.ReadUInt16(buffer, 14, bigEndian: false);

        if (inch == 0)
            return new ImageInfo(Format);

        // The placeable header expresses bounds in units-per-inch; pixel size assumes a conventional 96 DPI target.
        // A box stored with swapped corners (flipped logical axes) still spans the same extent.
        var width = Math.Abs(right - left) * 96 / inch;
        var height = Math.Abs(bottom - top) * 96 / inch;

        return new ImageInfo(Format, HeaderValues.PositiveOrNull(width), HeaderValues.PositiveOrNull(height), frameCount: 1);
    }

    private static bool Matches(byte[] header, byte[] magic)
    {
        for (var i = 0; i < magic.Length; i++)
        {
            if (header[i] != magic[i])
                return false;
        }
        return true;
    }
}
