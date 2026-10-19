namespace SqlArtisan.Internal;

// The percentile-fraction guard for PercentileCont/PercentileDisc: finite and
// within the SQL-standard 0..1 domain, universally invalid otherwise (ADR 0012).
internal static class PercentileFractionGuard
{
    internal static double Validate(double fraction, string construct)
    {
        if (!double.IsFinite(fraction))
        {
            throw new ArgumentException(
                $"{construct} requires a finite fraction.", nameof(fraction));
        }

        if (fraction is < 0 or > 1)
        {
            throw new ArgumentException(
                $"{construct} requires a fraction in the range 0 to 1.", nameof(fraction));
        }

        return fraction;
    }
}
