using System;
using System.Globalization;

namespace SqlArtisan.Internal;

internal static class GroupByItemResolver
{
    internal static SqlPart[] Resolve(object[] groupByItems)
    {
        if (groupByItems is null)
        {
            throw new ArgumentNullException(
                nameof(groupByItems), ExpressionResolver.NullValueMessage);
        }

        var resolved = new SqlPart[groupByItems.Length];

        for (int i = 0; i < groupByItems.Length; i++)
        {
            resolved[i] = Resolve(groupByItems[i]);
        }

        return resolved;
    }

    internal static SqlPart Resolve(object groupByItem)
    {
        if (groupByItem is null)
        {
            throw new ArgumentNullException(
                nameof(groupByItem), ExpressionResolver.NullValueMessage);
        }

        if (groupByItem is SqlExpression expr)
        {
            return expr;
        }
        else if (groupByItem is GroupingElement grouping)
        {
            return grouping;
        }
        else if (ExpressionResolver.IsNumeric(groupByItem))
        {
            return ResolveNumericGroupKey(groupByItem);
        }
        else
        {
            throw ExpressionResolver.UnresolvableValue("GroupByItem", groupByItem);
        }
    }

    private static readonly char[] FractionMarks = ['.', 'E', 'e'];

    // An ordinal below 1 names no select-list position and every engine refuses
    // it, so ADR 0012's three conditions hold and the throw is eager.
    private static NumericGroupKey ResolveNumericGroupKey(object value)
    {
        // Invariant, not ToString(): a comma-decimal culture would split "2.5"
        // into two group keys. IsNumeric guarantees IFormattable here.
        string text = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);

        switch (value)
        {
            case sbyte or byte or short or ushort or int or uint or nint or nuint
                or long or ulong:
                if (text == "0" || text.StartsWith('-'))
                {
                    throw new ArgumentException(
                        "A GROUP BY column ordinal must be 1 or greater.");
                }

                return new NumericGroupKey(text);

            case float or double or decimal:
                if ((value is double d && !double.IsFinite(d))
                    || (value is float f && !float.IsFinite(f)))
                {
                    throw new ArgumentException(
                        "A GROUP BY numeric group key must be finite.");
                }

                // A decimal point ("2" becomes "2.0") so a whole value cannot
                // re-read as an ordinal on the engines that take the constant.
                if (text.IndexOfAny(FractionMarks) < 0)
                {
                    text += ".0";
                }

                return new NumericGroupKey(text);

            default:
                throw ExpressionResolver.UnresolvableValue("GroupByItem", value);
        }
    }

    // The leading element is split from the `params` tail so a null tail array —
    // the C# binding for e.g. Rollup(a, null) — throws a named exception here
    // instead of an NRE when spread into a collection expression.
    internal static SqlPart[] ResolveElements(object element, params object[] elements)
    {
        if (elements is null)
        {
            throw new ArgumentNullException(
                nameof(elements), ExpressionResolver.NullValueMessage);
        }

        SqlPart[] resolved = new SqlPart[elements.Length + 1];
        resolved[0] = ResolveElement(element);

        for (int i = 0; i < elements.Length; i++)
        {
            resolved[i + 1] = ResolveElement(elements[i]);
        }

        return resolved;
    }

    private static SqlPart ResolveElement(object element) =>
        element is GroupingSet set ? set : ExpressionResolver.Resolve(element);
}
