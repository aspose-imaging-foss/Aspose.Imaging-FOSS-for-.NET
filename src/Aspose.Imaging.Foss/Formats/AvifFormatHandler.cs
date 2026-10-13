using System.IO;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class AvifFormatHandler : IFormatHandler
{
    public ImageFormat Format => ImageFormat.Avif;
    public int SignatureLength => 12;

    // AVIF is identified by an avif/avis brand, either as the major brand or, commonly, as a
    // compatible brand under a generic HEIF major brand such as mif1. A codec-specific HEIC
    // major brand wins over a stray compatible avif.
    public bool MatchesSignature(byte[] header, int headerLength)
    {
        if (!IsoBaseMediaBox.TryReadFileType(header, headerLength, out var major, out var compatible))
            return false;

        if (IsoBaseMediaBox.IsAvifBrand(major))
            return true;

        return !IsoBaseMediaBox.IsHeicBrand(major) && compatible.Exists(IsoBaseMediaBox.IsAvifBrand);
    }

    public ImageInfo ReadInfo(Stream stream)
    {
        var (w, h) = IsoBaseMediaBox.ReadImageSize(stream);
        return new ImageInfo(Format, w, h);
    }
}
