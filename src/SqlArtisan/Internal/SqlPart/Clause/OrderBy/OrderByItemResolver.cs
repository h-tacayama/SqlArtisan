using System.Globalization;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

internal static class OrderByItemResolver
{
    internal static SqlPart[] Resolve(object[] orderByItems)
    {
        if (orderByItems is null)
        {
            throw new ArgumentNullException(
                nameof(orderByItems), ExpressionResolver.NullValueMessage);
        }

        var resolved = new SqlPart[orderByItems.Length];

        for (int i = 0; i < orderByItems.Length; i++)
        {
            resolved[i] = Resolve(orderByItems[i]);
        }

        return resolved;
    }

    internal static SqlPart Resolve(object orderByItem)
    {
        if (orderByItem is null)
        {
            throw new ArgumentNullException(
                nameof(orderByItem), ExpressionResolver.NullValueMessage);
        }
        else if (orderByItem is SqlExpression expr)
        {
            return expr;
        }
        else if (orderByItem is ExpressionAlias alias)
        {
            return alias;
        }
        else if (orderByItem is SortOrder sortOrder)
        {
            return sortOrder;
        }
        else if (IsNumeric(orderByItem))
        {
            return ResolveNumericSortKey(orderByItem);
        }
        else if (IsBindable(orderByItem))
        {
            return new BindValue(orderByItem);
        }
        else
        {
            throw UnresolvableValue("OrderByItem", orderByItem);
        }
    }

    private static readonly char[] FractionMarks = ['.', 'E', 'e'];

    private static NumericSortKey ResolveNumericSortKey(object value)
    {
        // Plain ToString() is culture-dependent (comma-decimal cultures split
        // a single sort key into two tokens, e.g. "2.5" -> "2,5"); IsNumeric
        // guarantees IFormattable here.
        string text = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);

        switch (value)
        {
            case sbyte or byte or short or ushort or int or uint or nint or nuint
                or long or ulong:
                // A column ordinal, 1-based on every engine — ADR 0012's eager
                // value-domain class.
                if (text.StartsWith('-') || text == "0")
                {
                    throw new ArgumentException("An ORDER BY column ordinal must be positive.");
                }

                return new NumericSortKey(text, fractional: false);

            case float or double or decimal:
                // NaN/Infinity is a value-domain failure, not a type one:
                // no engine parses either as a sort-key literal.
                if ((value is double d && !double.IsFinite(d))
                    || (value is float f && !float.IsFinite(f)))
                {
                    throw new ArgumentException(
                        "An ORDER BY numeric sort key must be finite.");
                }

                // A literal sort key, rendered with a decimal point so a whole
                // value ("2.0" -> "2") cannot silently re-read as an ordinal.
                if (text.IndexOfAny(FractionMarks) < 0)
                {
                    text += ".0";
                }

                return new NumericSortKey(text, fractional: true);

            default:
                // Complex — a numeric with no sort-key rendering.
                throw UnresolvableValue("OrderByItem", value);
        }
    }
}
