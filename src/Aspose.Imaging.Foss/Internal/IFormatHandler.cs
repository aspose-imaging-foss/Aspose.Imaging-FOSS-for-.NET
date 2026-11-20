using System.Threading.Tasks;

namespace Aspose.Imaging.Foss.Internal;

internal interface IFormatHandler
{
    ImageFormat Format { get; }

    int SignatureLength { get; }

    bool MatchesSignature(byte[] header, int headerLength);

    Task<ImageInfo> ReadInfoAsync(ProbeReader stream);
}
