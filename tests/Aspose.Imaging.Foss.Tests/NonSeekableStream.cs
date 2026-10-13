using System;
using System.IO;

namespace Aspose.Imaging.Foss.Tests;

// Behaves like a network/request stream: forward-only reads, and any attempt to seek or
// query length/position throws, so tests catch code paths that assume seekability.
internal sealed class NonSeekableStream : MemoryStream
{
    public NonSeekableStream(byte[] data) : base(data)
    {
    }

    public override bool CanSeek => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
}
