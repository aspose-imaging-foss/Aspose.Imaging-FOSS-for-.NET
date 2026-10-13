using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aspose.Imaging.Foss.Tests;

// Deterministic mutation fuzzing over every sample format. Probe promises to never throw on
// malformed input, to finish promptly, and to report every number as either positive or unknown.
public class RobustnessTests
{
    private const int MutationsPerSample = 400;

    public static IEnumerable<object[]> Samples()
    {
        yield return new object[] { "png", SampleImages.Png(10, 10) };
        yield return new object[] { "jpeg", SampleImages.Jpeg(10, 10) };
        yield return new object[] { "gif", SampleImages.GifWithFrames(10, 10, 3) };
        yield return new object[] { "bmp", SampleImages.Bmp(10, 10) };
        yield return new object[] { "bmp-core", SampleImages.BmpCore(10, 10) };
        yield return new object[] { "webp-lossy", SampleImages.WebPLossy(10, 10) };
        yield return new object[] { "webp-lossless", SampleImages.WebPLossless(10, 10) };
        yield return new object[] { "webp-extended", SampleImages.WebPExtended(10, 10) };
        yield return new object[] { "ico", SampleImages.Ico(10, 10) };
        yield return new object[] { "tiff", SampleImages.TiffWithPages(10, 10, 8, 3) };
        yield return new object[] { "psd", SampleImages.Psd(10, 10) };
        yield return new object[] { "emf", SampleImages.Emf(10, 10) };
        yield return new object[] { "wmf", SampleImages.Wmf(10, 10) };
        yield return new object[] { "dicom", SampleImages.DicomWithPixelInfo(10, 10, 16) };
        yield return new object[] { "dicom-implicit", SampleImages.DicomWithPixelInfo(10, 10, 16, SampleImages.ImplicitVrLittleEndian) };
        yield return new object[] { "djvu", SampleImages.DjVu(10, 10) };
        yield return new object[] { "djvm", SampleImages.DjVuMultiPage(10, 10, 3) };
        yield return new object[] { "avif", SampleImages.Avif(10, 10) };
        yield return new object[] { "heic-grid", SampleImages.Heif(
            "heic", new[] { "mif1", "heic" },
            ispes: new[] { (512u, 512u), (4032u, 3024u) },
            primaryItemId: 1,
            associations: new[] { (1u, new[] { 3 }), (2u, new[] { 1, 2 }) }) };
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task MutatedInput_NeverThrowsHangsOrReportsNonPositiveValues(string name, byte[] sample)
    {
        // string.GetHashCode is randomized per process, so the seed is derived explicitly to keep
        // failures reproducible.
        var random = new Random(StableSeed(name));

        for (var i = 0; i < MutationsPerSample; i++)
        {
            var input = Mutate(sample, random);

            var task = Task.Run(() => (ImageProbe.Probe(input), ImageProbe.DetectFormat(input)));
            var finished = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(finished == task, $"{name} mutation {i} hung: {Convert.ToBase64String(input)}");

            var (info, detected) = await task;
            Assert.Equal(info.Format, detected);
            AssertPositiveOrNull(info.Width, name, i, input);
            AssertPositiveOrNull(info.Height, name, i, input);
            AssertPositiveOrNull(info.BitDepth, name, i, input);
            AssertPositiveOrNull(info.FrameCount, name, i, input);
        }
    }

    [Fact]
    public void RandomBytes_NeverThrow()
    {
        var random = new Random(20261013);
        for (var i = 0; i < 2000; i++)
        {
            var input = new byte[random.Next(0, 300)];
            random.NextBytes(input);

            var info = ImageProbe.Probe(input);
            Assert.Equal(info.Format, ImageProbe.DetectFormat(input));
        }
    }

    private static void AssertPositiveOrNull(int? value, string name, int iteration, byte[] input) =>
        Assert.True(value is null or > 0, $"{name} mutation {iteration} reported {value}: {Convert.ToBase64String(input)}");

    private static int StableSeed(string text)
    {
        var hash = 17;
        foreach (var c in text)
            hash = unchecked(hash * 31 + c);
        return hash;
    }

    private static byte[] Mutate(byte[] sample, Random random)
    {
        var data = new List<byte>(sample);
        var operations = random.Next(1, 6);

        for (var op = 0; op < operations; op++)
        {
            switch (random.Next(6))
            {
                case 0 when data.Count > 0:
                    data[random.Next(data.Count)] = (byte)random.Next(256);
                    break;
                case 1 when data.Count > 0:
                    data[random.Next(data.Count)] = random.Next(2) == 0 ? (byte)0xFF : (byte)0x00;
                    break;
                case 2 when data.Count > 1:
                    data.RemoveAt(random.Next(data.Count));
                    break;
                case 3:
                    data.Insert(random.Next(data.Count + 1), (byte)random.Next(256));
                    break;
                case 4 when data.Count > 4:
                    var at = random.Next(data.Count - 4);
                    for (var k = 0; k < 4; k++)
                        data[at + k] = 0xFF;
                    break;
                case 5 when data.Count > 1:
                    data = data.GetRange(0, random.Next(1, data.Count + 1)); // truncate
                    break;
            }
        }

        return data.ToArray();
    }
}
