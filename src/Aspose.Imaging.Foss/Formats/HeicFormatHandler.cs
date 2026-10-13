using System.IO;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class HeicFormatHandler : IFormatHandler
{
    public ImageFormat Format => ImageFormat.Heic;
    public int SignatureLength => 12;

    // Codec-specific HEIC major brands always match. The generic HEIF brands mif1/mif2 match
    // unless the compatible brands say the content is AVIF.
    public bool MatchesSignature(byte[] header, int headerLength)
    {
        if (!IsoBaseMediaBox.TryReadFileType(header, headerLength, out var major, out var compatible))
            return false;

        if (IsoBaseMediaBox.IsHeicBrand(major))
            return true;

        return (major == "mif1" || major == "mif2") && !compatible.Exists(IsoBaseMediaBox.IsAvifBrand);
    }

    public ImageInfo ReadInfo(Stream stream)
    {
        var (w, h) = IsoBaseMediaBox.ReadImageSize(stream);
        return new ImageInfo(Format, w, h);
    }
}
