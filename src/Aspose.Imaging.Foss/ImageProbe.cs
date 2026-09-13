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
    };

    private static readonly int SignatureBufferSize = ComputeSignatureBufferSize();

    public static ImageFormat DetectFormat(Stream stream) =>
        FindHandler(stream)?.Format ?? ImageFormat.Unknown;

    public static ImageFormat DetectFormat(byte[] data)
    {
        using var stream = new MemoryStream(data, writable: false);
        return DetectFormat(stream);
    }

    public static ImageInfo Probe(Stream stream)
    {
        var seekable = EnsureSeekable(stream);
        try
        {
            var handler = FindHandler(seekable);
            if (handler is null)
                return new ImageInfo(ImageFormat.Unknown);

            seekable.Seek(0, SeekOrigin.Begin);
            try
            {
                return handler.ReadInfo(seekable);
            }
            catch (Exception)
            {
                return new ImageInfo(handler.Format);
            }
        }
        finally
        {
            if (!ReferenceEquals(seekable, stream))
                seekable.Dispose();
        }
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
        var seekable = await EnsureSeekableAsync(stream, cancellationToken).ConfigureAwait(false);
        try
        {
            var handler = FindHandler(seekable);
            if (handler is null)
                return new ImageInfo(ImageFormat.Unknown);

            seekable.Seek(0, SeekOrigin.Begin);
            try
            {
                return handler.ReadInfo(seekable);
            }
            catch (Exception)
            {
                return new ImageInfo(handler.Format);
            }
        }
        finally
        {
            if (!ReferenceEquals(seekable, stream))
                seekable.Dispose();
        }
    }

    public static async Task<ImageInfo> ProbeFileAsync(string path, CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return await ProbeAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static IFormatHandler? FindHandler(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var buffer = new byte[SignatureBufferSize];
        var read = stream.ReadFully(buffer, 0, buffer.Length);

        foreach (var handler in Handlers)
        {
            if (handler.SignatureLength <= read && handler.MatchesSignature(buffer, read))
                return handler;
        }

        return null;
    }

    private static async Task<Stream> EnsureSeekableAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
            return stream;

        var buffered = new MemoryStream();
        await stream.CopyToAsync(buffered, 81920, cancellationToken).ConfigureAwait(false);
        buffered.Seek(0, SeekOrigin.Begin);
        return buffered;
    }

    private static Stream EnsureSeekable(Stream stream)
    {
        if (stream.CanSeek)
            return stream;

        var buffered = new MemoryStream();
        stream.CopyTo(buffered);
        buffered.Seek(0, SeekOrigin.Begin);
        return buffered;
    }

    private static int ComputeSignatureBufferSize()
    {
        var max = 0;
        foreach (var handler in Handlers)
            max = Math.Max(max, handler.SignatureLength);
        return max;
    }
}
