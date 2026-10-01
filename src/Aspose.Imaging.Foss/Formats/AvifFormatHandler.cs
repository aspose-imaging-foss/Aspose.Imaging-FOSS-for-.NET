using System.IO;
using System.Text;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class AvifFormatHandler : IFormatHandler
{
    public ImageFormat Format => ImageFormat.Avif;
    public int SignatureLength => 12;

    public bool MatchesSignature(byte[] header, int headerLength)
    {
        if (headerLength < 12) return false;
        if (header[4] != 'f' || header[5] != 't' || header[6] != 'y' || header[7] != 'p')
            return false;
        var brand = Encoding.ASCII.GetString(header, 8, 4);
        return brand == "avif" || brand == "avis";
    }

    public ImageInfo ReadInfo(Stream stream)
    {
        var (w, h) = IsoBaseMediaBox.ReadImageSize(stream);
        return new ImageInfo(Format, w, h);
    }
}
