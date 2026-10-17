using System.Globalization;

namespace SqlArtisan.Internal;

// PostgreSQL, the one engine with DISTINCT ON, reads an integer there as a select-list
// position, as ORDER BY does, so it renders as one; a bound value would be a constant
// that keeps a single row (#673).
internal static class DistinctOnItemResolver
{
    internal static SqlExpression[] Resolve(object[] items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items), ExpressionResolver.NullValueMessage);
        }

        var resolved = new SqlExpression[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            resolved[i] = items[i] is sbyte or byte or short or ushort or int or uint or nint
                or nuint or long or ulong
                ? ResolveOrdinal((IFormattable)items[i])
                : ExpressionResolver.ResolveKey(items[i], "DistinctOnItem");
        }

        return resolved;
    }

    private static NumericGroupKey ResolveOrdinal(IFormattable ordinal)
    {
        string text = ordinal.ToString(null, CultureInfo.InvariantCulture);

        // PostgreSQL 16 names no select-list position below 1 (live), so the
        // throw is eager, as GROUP BY's is.
        if (text == "0" || text.StartsWith('-'))
        {
            throw new ArgumentException("A DISTINCT ON column ordinal must be 1 or greater.");
        }

        return new NumericGroupKey(text);
    }
}
