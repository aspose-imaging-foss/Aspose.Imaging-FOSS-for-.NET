using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Imaging.Foss.Formats;
using Aspose.Imaging.Foss.Internal;

namespace Aspose.Imaging.Foss;

public static class ImageProbe
{
    private static readonly IFormatHandler[] Handlers =
    {
        new PngFormatHandler(),
        new JpegFormatHandler(),
        new GifFormatHandler(),
        new BmpFormatHandler(),
        new WebPFormatHandler(),
        new IcoFormatHandler(),
        new TiffFormatHandler(),
        new PsdFormatHandler(),
        new EmfFormatHandler(),
        new WmfFormatHandler(),
        new DicomFormatHandler(),
        new DjVuFormatHandler(),
        new AvifFormatHandler(),
        new HeicFormatHandler(),
    };

    private static readonly int SignatureBufferSize = ComputeSignatureBufferSize();

    public static ImageFormat DetectFormat(Stream stream)
    {
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        var header = new byte[SignatureBufferSize];
        var read = stream.ReadFully(header, 0, header.Length);
        return FindHandler(header, read)?.Format ?? ImageFormat.Unknown;
    }

    public static ImageFormat DetectFormat(byte[] data)
    {
        using var stream = new MemoryStream(data, writable: false);
        return DetectFormat(stream);
    }

    public static ImageInfo Probe(Stream stream)
    {
        using var buffer = stream.CanSeek ? null : new MemoryStream();
        if (buffer != null)
            stream.CopyTo(buffer);

        var reader = new ProbeReader(buffer ?? stream, useAsync: false);
        return ProbeSeekableAsync(reader, CancellationToken.None).GetAwaiter().GetResult();
    }

    public static ImageInfo Probe(byte[] data)
    {
        using var stream = new MemoryStream(data, writable: false);
        return Probe(stream);
    }

#if !NETSTANDARD2_0
    public static ImageInfo Probe(ReadOnlySpan<byte> data)
    {
        using var stream = new MemoryStream(data.ToArray(), writable: false);
        return Probe(stream);
    }
#endif

    public static ImageInfo Probe(FileInfo file) => ProbeFile(file.FullName);

    public static ImageInfo ProbeFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Probe(stream);
    }

    public static async Task<ImageInfo> ProbeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var buffer = stream.CanSeek ? null : new MemoryStream();
        if (buffer != null)
            await stream.CopyToAsync(buffer, 81920, cancellationToken).ConfigureAwait(false);

        var reader = new ProbeReader(buffer ?? stream, useAsync: true, cancellationToken);
        return await ProbeSeekableAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<ImageInfo> ProbeFileAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return await ProbeAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static IFormatHandler? FindHandler(byte[] buffer, int read)
    {
        foreach (var handler in Handlers)
        {
            if (handler.SignatureLength <= read && handler.MatchesSignature(buffer, read))
                return handler;
        }

        return null;
    }

    private static async Task<ImageInfo> ProbeSeekableAsync(ProbeReader reader, CancellationToken cancellationToken)
    {
        reader.Seek(0, SeekOrigin.Begin);
        var header = new byte[SignatureBufferSize];
        var read = await reader.ReadFullyAsync(header, 0, header.Length).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var handler = FindHandler(header, read);
        if (handler is null)
            return new ImageInfo(ImageFormat.Unknown);

        reader.Seek(0, SeekOrigin.Begin);
        try
        {
            var info = await handler.ReadInfoAsync(reader).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return info;
        }
        catch (Exception ex) when (ex is not OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return new ImageInfo(handler.Format);
        }
    }

    private static int ComputeSignatureBufferSize()
    {
        var max = 0;
        foreach (var handler in Handlers)
            max = Math.Max(max, handler.SignatureLength);
        return max;
    }
}
