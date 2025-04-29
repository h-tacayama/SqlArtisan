namespace SqlArtisan;

internal static class OrderByItemResolver
{
    internal static AbstractSqlPart[] Resolve(object[] orderByItems)
    {
        var resolved = new AbstractSqlPart[orderByItems.Length];

        for (int i = 0; i < orderByItems.Length; i++)
        {
            resolved[i] = Resolve(orderByItems[i]);
        }

        return resolved;
    }

    internal static AbstractSqlPart Resolve(object orderByItem)
    {
        if (orderByItem is AbstractExpr expr)
        {
            return expr;
        }
        else if (orderByItem is ExprAlias alias)
        {
            return alias;
        }
        else if (orderByItem is SortOrder sortOrder)
        {
            return sortOrder;
        }
        else if (ExprResolver.IsNumeric(orderByItem))
        {
            return new Literal(orderByItem.ToString() ?? "");
        }
        else if (ExprResolver.IsBindable(orderByItem))
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
