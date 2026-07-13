namespace SqlArtisan.Internal;

internal static class PercentileFraction
{
    internal static double Validate(double fraction)
    {
        if (!double.IsFinite(fraction))
        {
            throw new ArgumentException(
                "The percentile fraction must be a finite number.",
                nameof(fraction));
        }

        if (fraction is < 0 or > 1)
        {
            throw new ArgumentException(
                "The percentile fraction must be in the range 0 to 1.",
                nameof(fraction));
        }

        return fraction;
    }
}
