using System.Globalization;

namespace SqlArtisan.Internal;

internal static class Int32Extensions
{
    internal static string ToInvariantString(this int value) =>
        value.ToString(CultureInfo.InvariantCulture);
}
