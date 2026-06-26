using System.Globalization;

namespace SqlArtisan.Internal;

internal static class DoubleExtensions
{
    internal static string ToInvariantString(this double value) =>
        value.ToString(CultureInfo.InvariantCulture);
}
