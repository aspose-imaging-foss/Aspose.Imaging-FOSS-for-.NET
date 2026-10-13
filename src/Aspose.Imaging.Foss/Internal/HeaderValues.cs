namespace Aspose.Imaging.Foss.Internal;

internal static class HeaderValues
{
    // Every number ImageInfo reports (dimensions, bit depth, frame count) is meaningful only when
    // positive. Zero, negative or int-overflowing header fields mean the header is malformed or
    // defers the value elsewhere, so they are reported as unknown rather than passed through.
    public static int? PositiveOrNull(long? value) =>
        value > 0 && value <= int.MaxValue ? (int)value.Value : null;
}
