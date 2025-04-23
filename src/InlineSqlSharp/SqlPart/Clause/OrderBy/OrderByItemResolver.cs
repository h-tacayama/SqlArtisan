namespace InlineSqlSharp;

internal static class OrderByItemResolver
{
    internal static AbstractSqlPart[] Resolve(object[] orderByItems) =>
        orderByItems.Select(Resolve).ToArray();

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
