using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

internal static class OrderByItemResolver
{
    internal static SqlPart[] Resolve(object[] orderByItems)
    {
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
            return new LiteralValue(orderByItem.ToString() ?? "");
        }
        else if (IsBindable(orderByItem))
        {
            return new BindValue(orderByItem);
        }
        else
        {
            throw new ArgumentException(
                $"Invalid type for OrderByItem: {orderByItem.GetType()}");
        }
    }
}
