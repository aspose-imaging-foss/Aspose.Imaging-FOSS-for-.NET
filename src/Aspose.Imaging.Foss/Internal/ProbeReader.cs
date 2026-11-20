using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aspose.Imaging.Foss.Internal;

// Each probe owns one reader. Both entry points use the same parser and seek offsets.
internal sealed class ProbeReader
{
    private readonly Stream stream;
    private readonly bool useAsync;
    private readonly CancellationToken cancellationToken;
    private readonly byte[] singleByte = new byte[1];

    public ProbeReader(Stream stream, bool useAsync, CancellationToken cancellationToken = default)
    {
        this.stream = stream;
        this.useAsync = useAsync;
        this.cancellationToken = cancellationToken;
    }

    public long Length => stream.Length;
    public long Position => stream.Position;

    public void Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);

    public async Task<int> ReadFullyAsync(byte[] buffer, int offset, int count)
    {
        if (!useAsync)
            return stream.ReadFully(buffer, offset, count);

        var totalRead = 0;
        while (totalRead < count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
                break;
            totalRead += read;
        }
        return totalRead;
    }

    public async Task<int> ReadByteAsync()
    {
        if (!useAsync)
            return stream.ReadByte();

        return await ReadFullyAsync(singleByte, 0, 1).ConfigureAwait(false) == 1 ? singleByte[0] : -1;
    }
}
