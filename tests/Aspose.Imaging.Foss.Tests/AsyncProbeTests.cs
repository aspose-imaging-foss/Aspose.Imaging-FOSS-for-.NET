using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aspose.Imaging.Foss.Tests;

public class AsyncProbeTests
{
    [Theory]
    [MemberData(nameof(RobustnessTests.Samples), MemberType = typeof(RobustnessTests))]
    public async Task SeekableStream_ReadsAsynchronouslyAndMatchesSyncProbe(string name, byte[] data)
    {
        var expected = ImageProbe.Probe(data);
        using var stream = new AsyncOnlyStream(data);
        var actual = await ImageProbe.ProbeAsync(stream);

        Assert.True(expected.Format == actual.Format, name);
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        Assert.Equal(expected.BitDepth, actual.BitDepth);
        Assert.Equal(expected.FrameCount, actual.FrameCount);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task PreCancelledSeekableStream_IsLeftAtItsOriginalPosition()
    {
        using var stream = new MemoryStream(SampleImages.Png(80, 60)) { Position = 3 };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ImageProbe.ProbeAsync(stream, new CancellationToken(true)));
        Assert.Equal(3, stream.Position);
    }

    [Fact]
    public async Task PreCancelledFileProbe_ObservesCancellationBeforeOpeningTheFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ImageProbe.ProbeFileAsync(path, new CancellationToken(true)));
    }

    [Fact]
    public async Task CancellingDuringMetadataRead_PropagatesToTheCaller()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new AsyncOnlyStream(SampleImages.DicomWithPixelInfo(80, 60, 16),
            (position, token) =>
            {
                if (position >= 132) // First data after the DICOM signature.
                {
                    Assert.Equal(cts.Token, token);
                    cts.Cancel();
                    token.ThrowIfCancellationRequested();
                }
            });

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ImageProbe.ProbeAsync(stream, cts.Token));
        Assert.Equal(cts.Token, error.CancellationToken);
    }

    [Fact]
    public async Task SeekablePng_OnlyReadsHeaderData()
    {
        var data = SampleImages.Png(80, 60);
        Array.Resize(ref data, 1024 * 1024);
        using var stream = new AsyncOnlyStream(data);
        var info = await ImageProbe.ProbeAsync(stream);
        Assert.Equal(80, info.Width);
        Assert.InRange(stream.BytesRead, 25, 4096);
    }

    private sealed class AsyncOnlyStream(byte[] data, Action<long, CancellationToken>? beforeRead = null)
        : MemoryStream(data)
    {
        public int BytesRead { get; private set; }

        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException();
        public override int ReadByte() => throw new InvalidOperationException();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            await Task.Yield();
            beforeRead?.Invoke(Position, token);
            var read = base.Read(buffer, offset, Math.Min(7, count));
            BytesRead += read;
            return read;
        }
    }
}
