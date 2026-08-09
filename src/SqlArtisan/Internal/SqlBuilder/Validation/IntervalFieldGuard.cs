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

    internal static void ValidateRange(IntervalField leading, IntervalField trailing)
    {
        if (!ValidRanges.Contains((leading.Field, trailing.Field)))
        {
            throw new ArgumentException(
                $"INTERVAL {DatepartKeywords.Of(leading.Field)} TO {DatepartKeywords.Of(trailing.Field)} is not a valid field range.");
        }
    }
}
