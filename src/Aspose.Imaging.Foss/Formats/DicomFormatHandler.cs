using System.Collections.Generic;
using System.IO;
using System.Text;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

/// <summary>
/// Detects standard DICOM files (128-byte preamble + "DICM" magic) and reads Rows/Columns/
/// BitsAllocated from the data set. Handles Implicit VR Little Endian and Explicit VR
/// Little/Big Endian transfer syntaxes; any other transfer syntax (including the common
/// compressed ones) is treated as Explicit VR Little Endian, which is how their non-pixel
/// elements are actually encoded. Sequence/encapsulated elements with an undefined length
/// stop the walk rather than being parsed, since Rows/Columns/BitsAllocated always precede
/// them in conformant files.
/// </summary>
internal sealed class DicomFormatHandler : IFormatHandler
{
    private const string ImplicitVrLittleEndian = "1.2.840.10008.1.2";
    private const string ExplicitVrBigEndian = "1.2.840.10008.1.2.2";
    private const uint UndefinedLength = 0xFFFFFFFF;
    private const uint MaxUidLength = 64;

    // VRs that use the "long form" explicit encoding: VR (2) + reserved (2) + length (4).
    private static readonly HashSet<string> LongFormVrs = new(new[]
    {
        "OB", "OW", "OF", "OD", "OL", "SQ", "UC", "UR", "UT", "UN"
    });

    public ImageFormat Format => ImageFormat.Dicom;
    public int SignatureLength => 132;

    public bool MatchesSignature(byte[] header, int headerLength) =>
        Encoding.ASCII.GetString(header, 128, 4) == "DICM";

    public ImageInfo ReadInfo(Stream stream)
    {
        stream.Seek(132, SeekOrigin.Begin);

        var transferSyntax = ReadFileMetaTransferSyntax(stream);
        var implicitVr = transferSyntax == ImplicitVrLittleEndian;
        var bigEndian = transferSyntax == ExplicitVrBigEndian;

        int? rows = null;
        int? columns = null;
        int? bitsAllocated = null;

        var tagBuf = new byte[4];
        var remainingElements = 100_000;

        while (remainingElements-- > 0 && (rows is null || columns is null || bitsAllocated is null))
        {
            if (stream.ReadFully(tagBuf, 0, 4) < 4)
                break;

            var group = EndianReader.ReadUInt16(tagBuf, 0, bigEndian);
            var element = EndianReader.ReadUInt16(tagBuf, 2, bigEndian);

            if (group == 0x7FE0 && element == 0x0010) // PixelData: image metadata is behind us
                break;

            if (!TryReadLength(stream, implicitVr, bigEndian, out var length))
                break;

            if (length == UndefinedLength)
                break; // undefined-length sequence/encapsulated data; not walked

            if (group == 0x0028 && length == 2 &&
                (element == 0x0010 || element == 0x0011 || element == 0x0100))
            {
                var valueBuf = new byte[2];
                if (stream.ReadFully(valueBuf, 0, 2) < 2)
                    break;

                var value = EndianReader.ReadUInt16(valueBuf, 0, bigEndian);
                switch (element)
                {
                    case 0x0010: rows = value; break;
                    case 0x0011: columns = value; break;
                    case 0x0100: bitsAllocated = value; break;
                }
            }
            else
            {
                stream.Seek(length, SeekOrigin.Current);
            }
        }

        return new ImageInfo(Format,
            width: HeaderValues.PositiveOrNull(columns),
            height: HeaderValues.PositiveOrNull(rows),
            bitDepth: HeaderValues.PositiveOrNull(bitsAllocated));
    }

    // File Meta Information (group 0002) is always Explicit VR Little Endian, regardless of
    // the main data set's transfer syntax. Reads through it looking for the Transfer Syntax
    // UID (0002,0010), stopping - and rewinding - as soon as an element outside group 0002
    // is seen, since that's the first element of the main data set.
    private static string? ReadFileMetaTransferSyntax(Stream stream)
    {
        var tagBuf = new byte[4];

        while (true)
        {
            var elementStart = stream.Position;
            if (stream.ReadFully(tagBuf, 0, 4) < 4)
                return null;

            var group = EndianReader.ReadUInt16(tagBuf, 0, bigEndian: false);
            var element = EndianReader.ReadUInt16(tagBuf, 2, bigEndian: false);

            if (group != 0x0002)
            {
                stream.Seek(elementStart, SeekOrigin.Begin);
                return null;
            }

            if (!TryReadLength(stream, implicitVr: false, bigEndian: false, out var length) || length == UndefinedLength)
                return null;

            if (group == 0x0002 && element == 0x0010)
            {
                // A UID is at most 64 characters; a larger length is malformed and must not
                // drive an allocation sized by untrusted input.
                if (length > MaxUidLength)
                    return null;

                var valueBuf = new byte[length];
                if (stream.ReadFully(valueBuf, 0, (int)length) < length)
                    return null;

                return Encoding.ASCII.GetString(valueBuf).TrimEnd('\0', ' ');
            }

            stream.Seek(length, SeekOrigin.Current);
        }
    }

    private static bool TryReadLength(Stream stream, bool implicitVr, bool bigEndian, out uint length)
    {
        length = 0;

        if (implicitVr)
        {
            var lenBuf = new byte[4];
            if (stream.ReadFully(lenBuf, 0, 4) < 4)
                return false;

            length = EndianReader.ReadUInt32(lenBuf, 0, bigEndian);
            return true;
        }

        var vrBuf = new byte[2];
        if (stream.ReadFully(vrBuf, 0, 2) < 2)
            return false;

        var vr = Encoding.ASCII.GetString(vrBuf, 0, 2);

        if (LongFormVrs.Contains(vr))
        {
            var reserved = new byte[2];
            var lenBuf = new byte[4];
            if (stream.ReadFully(reserved, 0, 2) < 2 || stream.ReadFully(lenBuf, 0, 4) < 4)
                return false;

            length = EndianReader.ReadUInt32(lenBuf, 0, bigEndian);
        }
        else
        {
            var lenBuf = new byte[2];
            if (stream.ReadFully(lenBuf, 0, 2) < 2)
                return false;

            length = EndianReader.ReadUInt16(lenBuf, 0, bigEndian);
        }

        return true;
    }
}
