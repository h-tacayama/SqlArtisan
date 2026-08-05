namespace SqlArtisan.Internal;

// Value-domain guards for NTILE, NTH_VALUE, and window-frame bounds (ADR 0012):
// every check here is literal-embedded, call-site-fixed, and invalid on every
// engine, so each one is eager rather than a Build(Dbms) guard.
internal static class WindowFrameGuard
{
    internal static int ValidateOffset(int offset, string boundKeyword)
    {
        if (offset < 0)
        {
            throw new ArgumentException($"{boundKeyword} requires a non-negative offset.");
        }

        return offset;
    }

    internal static int ValidateNtileBuckets(int buckets)
    {
        if (buckets <= 0)
        {
            throw new ArgumentException($"{Keywords.Ntile} requires a positive bucket count.");
        }

        return buckets;
    }

    internal static int ValidateNthValuePosition(int position)
    {
        if (position <= 0)
        {
            throw new ArgumentException($"{Keywords.NthValue} requires a positive position.");
        }

        return position;
    }

    internal static void ValidateBetween(FrameBound start, FrameBound end)
    {
        if (start.Kind > end.Kind)
        {
            throw new ArgumentException(
                "A window frame's BETWEEN start bound must not be later than its end bound.");
        }
    }

    // A single bound is shorthand for BETWEEN bound AND CURRENT ROW, so the
    // same ordering rule applies with CURRENT ROW as the implicit end.
    internal static void ValidateSoleExtent(FrameBound bound)
    {
        if (bound.Kind > FrameBoundKind.CurrentRow)
        {
            throw new ArgumentException(
                "A window frame with a single bound must not be later than CURRENT ROW.");
        }
    }
}
