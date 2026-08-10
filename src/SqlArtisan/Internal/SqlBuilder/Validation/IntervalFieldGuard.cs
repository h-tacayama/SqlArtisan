namespace SqlArtisan.Internal;

// Oracle interval literal field rules (ADR 0012). Each is call-site-fixed and
// true wherever this literal shape is accepted at all, so each is eager. A
// precision-bearing sole SECOND is deliberately *not* guarded: it renders
// Oracle's leading precision, so it fails ADR 0012's universally-invalid test.
internal static class IntervalFieldGuard
{
    private static readonly HashSet<(DateTimePart Leading, DateTimePart Trailing)> ValidRanges =
    [
        (DateTimePart.Year, DateTimePart.Month),
        (DateTimePart.Day, DateTimePart.Hour),
        (DateTimePart.Day, DateTimePart.Minute),
        (DateTimePart.Day, DateTimePart.Second),
        (DateTimePart.Hour, DateTimePart.Minute),
        (DateTimePart.Hour, DateTimePart.Second),
        (DateTimePart.Minute, DateTimePart.Second),
    ];

    internal static void ValidatePrecision(int precision, DateTimePart field)
    {
        if (precision is < 0 or > 9)
        {
            throw new ArgumentException(
                $"{DatepartKeywords.Of(field)} precision must be between 0 and 9.");
        }
    }

    internal static void ValidateRange(IntervalField leading, IntervalField trailing)
    {
        if (!ValidRanges.Contains((leading.Field, trailing.Field)))
        {
            throw new ArgumentException(
                $"INTERVAL {DatepartKeywords.Of(leading.Field)} TO {DatepartKeywords.Of(trailing.Field)} is not a valid field range.");
        }

        // A trailing precision is the fractional-seconds count, so it exists only on
        // SECOND; every other trailing field carrying one is invalid on every dialect.
        if (trailing.HasPrecision && trailing.Field != DateTimePart.Second)
        {
            throw new ArgumentException(
                $"A trailing {DatepartKeywords.Of(trailing.Field)} in an INTERVAL range does not support a precision; only TO SECOND does.");
        }
    }
}
