using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;

namespace Aspose.Imaging.Foss.Tests;

public class AnimationFrameCountTests
{
    // Independently encoded by Pillow. The PNG also has a separate default image.
    [Theory]
    [InlineData("animation.png", ImageFormat.Png, 2)]
    [InlineData("animation.webp", ImageFormat.WebP, 3)]
    public async Task EncodedAnimation_ReportsAnimationFrames(string file, ImageFormat format, int frames)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", file);
        foreach (var info in new[] { ImageProbe.ProbeFile(path), await ImageProbe.ProbeFileAsync(path) })
        {
            Assert.Equal(format, info.Format);
            Assert.Equal(12, info.Width);
            Assert.Equal(7, info.Height);
            Assert.Equal(frames, info.FrameCount);
        }
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(0x80000000u)]
    public void Apng_InvalidDeclaredCount_ReturnsUnknown(uint count)
    {
        var data = ReadFixture("animation.png");
        var countOffset = data.AsSpan().IndexOf("acTL"u8) + 4;
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(countOffset), count);
        Assert.Null(ImageProbe.Probe(data).FrameCount);
    }

    [Fact]
    public void Apng_IncompleteAnimationControl_PreservesDimensions()
    {
        var data = ReadFixture("animation.png");
        var countOffset = data.AsSpan().IndexOf("acTL"u8) + 4;
        var info = ImageProbe.Probe(data[..(countOffset + 4)]);
        Assert.Equal(12, info.Width);
        Assert.Equal(7, info.Height);
        Assert.Null(info.FrameCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WebP_IncompleteLastFrame_PreservesCanvasAndReturnsUnknownCount(bool updateRiffSize)
    {
        var data = ReadFixture("animation.webp");
        var truncated = data[..(LastWebPFrameOffset(data) + 12)];
        if (updateRiffSize)
            BinaryPrimitives.WriteUInt32LittleEndian(truncated.AsSpan(4), (uint)(truncated.Length - 8));
        var info = ImageProbe.Probe(truncated);
        Assert.Equal(12, info.Width);
        Assert.Equal(7, info.Height);
        Assert.Null(info.FrameCount);
    }

    [Fact]
    public void WebP_OnlyCountsFramesInsideDeclaredRiff()
    {
        var data = ReadFixture("animation.webp");
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), (uint)(LastWebPFrameOffset(data) - 8));
        Assert.Equal(2, ImageProbe.Probe(data).FrameCount);
    }

    private static int LastWebPFrameOffset(byte[] data)
    {
        // The trailing, odd-sized XMP payload contains the literal text "ANMF!".
        var xmp = data.AsSpan().IndexOf("XMP "u8);
        return data.AsSpan(0, xmp).LastIndexOf("ANMF"u8);
    }

    private static byte[] ReadFixture(string file) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", file));
}
