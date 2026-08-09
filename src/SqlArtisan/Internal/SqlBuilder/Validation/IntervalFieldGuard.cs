namespace SqlArtisan.Internal;

// Oracle interval literal field rules (ADR 0012): precision is fixed 0-9 and
// only seven field pairings exist, both call-site-fixed facts true wherever
// this literal shape is accepted at all — so both are eager guards.
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

    // Oracle's standalone SECOND precision is a (leading, fractional) pair, not the
    // single value every other field takes — deliberately unsupported, so reject the
    // one marker that can reach this position carrying a precision (ToSecond).
    internal static void ValidateSoleField(IntervalField field)
    {
        if (field.Field == DateTimePart.Second && field.HasPrecision)
        {
            throw new ArgumentException(
                "INTERVAL SECOND does not support a precision; use Second() without one.");
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
