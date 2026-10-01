using System.IO;
using System.Text;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss.Formats;

internal sealed class HeicFormatHandler : IFormatHandler
{
    public ImageFormat Format => ImageFormat.Heic;
    public int SignatureLength => 12;

    public bool MatchesSignature(byte[] header, int headerLength)
    {
        if (headerLength < 12) return false;
        if (header[4] != 'f' || header[5] != 't' || header[6] != 'y' || header[7] != 'p')
            return false;
        var brand = Encoding.ASCII.GetString(header, 8, 4);
        return brand == "heic" || brand == "heix" || brand == "hevc" || brand == "hevx"
            || brand == "heim" || brand == "heis" || brand == "hevm" || brand == "hevs"
            || brand == "mif1" || brand == "mif2";
    }

    public ImageInfo ReadInfo(Stream stream)
    {
        var (w, h) = IsoBaseMediaBox.ReadImageSize(stream);
        return new ImageInfo(Format, w, h);
    }
}
