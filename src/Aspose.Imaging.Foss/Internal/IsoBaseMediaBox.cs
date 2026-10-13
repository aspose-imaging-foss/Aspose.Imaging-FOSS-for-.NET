using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Aspose.Imaging.Foss.Internal;

// Helpers for ISO Base Media File Format (ISOBMFF) box walking, shared by AVIF and HEIC handlers.
internal static class IsoBaseMediaBox
{
    // ipma property indices are at most 15 bits, so later ipco children can't be referenced.
    private const int MaxPropertyIndex = 0x7FFF;

    // Real ipma boxes are a few KB even for heavily tiled images; anything far larger is not
    // worth buffering, and the size falls back to the largest ispe.
    private const long MaxIpmaLength = 1024 * 1024;

    // Codec-specific HEIF brands (HEVC images/sequences). mif1/mif2 are generic HEIF brands that
    // AVIF files also use as their major brand, so they don't identify the codec on their own.
    private static readonly HashSet<string> HeicBrands = new()
    {
        "heic", "heix", "hevc", "hevx", "heim", "heis", "hevm", "hevs"
    };

    private static readonly HashSet<string> AvifBrands = new() { "avif", "avis" };

    internal static bool IsHeicBrand(string brand) => HeicBrands.Contains(brand);

    internal static bool IsAvifBrand(string brand) => AvifBrands.Contains(brand);

    // Reads the ftyp box that opens the file: the major brand plus whichever compatible brands
    // fit in the signature buffer.
    internal static bool TryReadFileType(byte[] header, int headerLength,
        out string majorBrand, out List<string> compatibleBrands)
    {
        majorBrand = string.Empty;
        compatibleBrands = new List<string>();

        if (headerLength < 12 || Encoding.ASCII.GetString(header, 4, 4) != "ftyp")
            return false;

        majorBrand = Encoding.ASCII.GetString(header, 8, 4);

        // size(4) + "ftyp"(4) + major brand(4) + minor version(4), then 4-byte compatible brands.
        // A size of 0 runs to the end of the file; 1 (64-bit size) never occurs for ftyp in practice.
        var boxSize = EndianReader.ReadUInt32(header, 0, bigEndian: true);
        var end = boxSize == 0 || boxSize > headerLength ? headerLength : (int)boxSize;

        for (var offset = 16; offset + 4 <= end; offset += 4)
            compatibleBrands.Add(Encoding.ASCII.GetString(header, offset, 4));

        return true;
    }

    // Reads the primary image's dimensions from its ispe property: meta → pitm gives the primary
    // item, iprp → ipma maps it to ipco property indices, one of which is its ispe. This matters
    // for grid images (e.g. iPhone HEIC), where ipco also holds a smaller ispe for the tiles.
    // When the association can't be resolved, the largest ispe is used: tiles, thumbnails and
    // auxiliary images are never larger than the image they belong to.
    internal static (int? width, int? height) ReadImageSize(Stream stream)
    {
        if (!TryFindChild(stream, 0, stream.Length, "meta", out var meta))
            return (null, null);

        // meta is a FullBox: version(1) + flags(3) precede nested boxes.
        var metaChildrenStart = meta.DataStart + 4;

        if (!TryFindChild(stream, metaChildrenStart, meta.End, "iprp", out var iprp))
            return (null, null);

        if (!TryFindChild(stream, iprp.DataStart, iprp.End, "ipco", out var ipco))
            return (null, null);

        var sizes = ReadIspeProperties(stream, ipco);
        if (sizes.Count == 0)
            return (null, null);

        (uint width, uint height)? chosen = null;

        if (TryFindChild(stream, metaChildrenStart, meta.End, "pitm", out var pitm) &&
            ReadPrimaryItemId(stream, pitm) is uint primaryItemId &&
            FindItemIspeIndex(stream, iprp, primaryItemId, sizes) is int index)
        {
            chosen = sizes[index];
        }

        chosen ??= Largest(sizes.Values);

        return (HeaderValues.PositiveOrNull(chosen.Value.width), HeaderValues.PositiveOrNull(chosen.Value.height));
    }

    // Maps each ispe in ipco to its 1-based property index, as ipma refers to them.
    private static Dictionary<int, (uint width, uint height)> ReadIspeProperties(Stream stream, Box ipco)
    {
        var sizes = new Dictionary<int, (uint, uint)>();
        var index = 0;

        foreach (var property in ReadChildren(stream, ipco.DataStart, ipco.End))
        {
            if (++index > MaxPropertyIndex)
                break;

            if (property.Type != "ispe")
                continue;

            // ispe is a FullBox: version(1) + flags(3) + image_width(4 BE) + image_height(4 BE).
            var data = ReadData(stream, property, maxLength: 64);
            if (data is null || data.Length < 12)
                continue;

            sizes[index] = (EndianReader.ReadUInt32(data, 4, bigEndian: true),
                EndianReader.ReadUInt32(data, 8, bigEndian: true));
        }

        return sizes;
    }

    private static uint? ReadPrimaryItemId(Stream stream, Box pitm)
    {
        // FullBox header(4), then item_ID: 16-bit in version 0, 32-bit in version 1.
        var data = ReadData(stream, pitm, maxLength: 64);
        if (data is null || data.Length < 6)
            return null;

        if (data[0] == 0)
            return EndianReader.ReadUInt16(data, 4, bigEndian: true);

        return data.Length < 8 ? null : EndianReader.ReadUInt32(data, 4, bigEndian: true);
    }

    // Returns the index of the first ispe that an ipma box associates with the item.
    private static int? FindItemIspeIndex(Stream stream, Box iprp, uint itemId,
        Dictionary<int, (uint, uint)> sizes)
    {
        foreach (var ipma in ReadChildren(stream, iprp.DataStart, iprp.End))
        {
            if (ipma.Type != "ipma")
                continue;

            var data = ReadData(stream, ipma, MaxIpmaLength);
            if (data is not null && FindIspeIndexInIpma(data, itemId, sizes) is int index)
                return index;
        }

        return null;
    }

    private static int? FindIspeIndexInIpma(byte[] data, uint itemId, Dictionary<int, (uint, uint)> sizes)
    {
        // FullBox: version(1) + flags(3). Version >= 1 widens item_ID to 32 bits; flag bit 0
        // widens each association to 16 bits (1 essential bit + 15-bit property index).
        if (data.Length < 8)
            return null;

        var version = data[0];
        var wideIndices = (data[3] & 1) != 0;
        var itemIdSize = version < 1 ? 2 : 4;
        var associationSize = wideIndices ? 2 : 1;

        var entryCount = EndianReader.ReadUInt32(data, 4, bigEndian: true);
        var position = 8;

        for (uint entry = 0; entry < entryCount; entry++)
        {
            if (position + itemIdSize + 1 > data.Length)
                return null;

            var entryItemId = itemIdSize == 2
                ? EndianReader.ReadUInt16(data, position, bigEndian: true)
                : EndianReader.ReadUInt32(data, position, bigEndian: true);
            position += itemIdSize;

            int associationCount = data[position++];
            if (position + associationCount * associationSize > data.Length)
                return null;

            if (entryItemId != itemId)
            {
                position += associationCount * associationSize;
                continue;
            }

            for (var i = 0; i < associationCount; i++)
            {
                var propertyIndex = wideIndices
                    ? EndianReader.ReadUInt16(data, position, bigEndian: true) & 0x7FFF
                    : data[position] & 0x7F;
                position += associationSize;

                if (sizes.ContainsKey(propertyIndex))
                    return propertyIndex;
            }

            return null;
        }

        return null;
    }

    private static (uint width, uint height) Largest(IEnumerable<(uint width, uint height)> sizes)
    {
        (uint width, uint height) largest = default;
        ulong largestArea = 0;

        foreach (var size in sizes)
        {
            var area = (ulong)size.width * size.height;
            if (area >= largestArea)
            {
                largest = size;
                largestArea = area;
            }
        }

        return largest;
    }

    private static bool TryFindChild(Stream stream, long start, long end, string type, out Box box)
    {
        foreach (var child in ReadChildren(stream, start, end))
        {
            if (child.Type == type)
            {
                box = child;
                return true;
            }
        }

        box = default;
        return false;
    }

    // Reads a box's payload, or null when it is larger than the caller is willing to buffer.
    private static byte[]? ReadData(Stream stream, Box box, long maxLength)
    {
        var length = box.End - box.DataStart;
        if (length < 0 || length > maxLength)
            return null;

        var data = new byte[length];
        stream.Seek(box.DataStart, SeekOrigin.Begin);
        var read = stream.ReadFully(data, 0, data.Length);
        if (read < data.Length)
            Array.Resize(ref data, read);

        return data;
    }

    // Enumerates the boxes in [start, end). Each box is clamped to its parent's end, so a
    // truncated or overstated child can't send the walk outside the region. Every step advances
    // by at least the 8-byte header, so the walk always terminates.
    private static IEnumerable<Box> ReadChildren(Stream stream, long start, long end)
    {
        var header = new byte[16];
        var position = start;

        while (position + 8 <= end)
        {
            stream.Seek(position, SeekOrigin.Begin);
            if (stream.ReadFully(header, 0, 8) < 8)
                yield break;

            var sizeField = EndianReader.ReadUInt32(header, 0, bigEndian: true);
            var type = Encoding.ASCII.GetString(header, 4, 4);

            long dataStart = position + 8;
            long boxEnd;

            if (sizeField == 1)
            {
                // 64-bit extended size immediately follows the 8-byte header.
                if (stream.ReadFully(header, 8, 8) < 8)
                    yield break;

                var extSize = ((ulong)EndianReader.ReadUInt32(header, 8, bigEndian: true) << 32)
                    | EndianReader.ReadUInt32(header, 12, bigEndian: true);

                if (extSize < 16)
                    yield break;

                dataStart = position + 16;
                boxEnd = extSize > (ulong)(end - position) ? end : position + (long)extSize;
            }
            else if (sizeField == 0)
            {
                boxEnd = end;
            }
            else
            {
                if (sizeField < 8)
                    yield break;

                boxEnd = sizeField > end - position ? end : position + sizeField;
            }

            yield return new Box(type, dataStart, boxEnd);

            position = boxEnd;
        }
    }

    private readonly struct Box
    {
        public Box(string type, long dataStart, long end)
        {
            Type = type;
            DataStart = dataStart;
            End = end;
        }

        public string Type { get; }
        public long DataStart { get; }
        public long End { get; }
    }
}
