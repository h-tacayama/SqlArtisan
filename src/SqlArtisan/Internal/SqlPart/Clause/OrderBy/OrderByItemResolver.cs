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
        if (orderByItem is SqlExpression expr)
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
            // Plain ToString() is culture-dependent (comma-decimal cultures split
            // a single sort key into two tokens, e.g. "2.5" -> "2,5"); IsNumeric
            // guarantees IFormattable here.
            string text = ((IFormattable)orderByItem).ToString(null, CultureInfo.InvariantCulture);
            return new LiteralValue(text);
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
}
