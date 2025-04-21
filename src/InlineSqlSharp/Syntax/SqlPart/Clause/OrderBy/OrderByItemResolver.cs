namespace InlineSqlSharp;

internal static class OrderByItemResolver
{
    internal static OrderByItem[] Resolve(object[] items) =>
        items.Select(Resolve).ToArray();

    internal static OrderByItem Resolve(object item)
    {
        if (item is AbstractExpr expr)
        {
            return new(expr);
        }
        else if (item is ExprAlias alias)
        {
            return new(alias);
        }
        else if (item is SortOrder sortOrder)
        {
            return new(sortOrder);
        }
        else if (ExprRsolver.IsNumeric(item))
        {
            return new(new Literal(item.ToString() ?? ""));
        }
        else if (ExprRsolver.IsBindable(item))
        {
            return new(new BindValue(item));
        }
        else
        {
            throw new ArgumentException(
                $"Invalid type for OrderByItem: {item.GetType()}");
        }
    }
}
