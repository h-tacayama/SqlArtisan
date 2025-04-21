namespace InlineSqlSharp;

internal static class SelectItemResolver
{
    internal static SelectItem[] Resolve(object[] items) =>
        items.Select(Resolve).ToArray();

    internal static SelectItem Resolve(object item)
    {
        if (item is AbstractExpr expr)
        {
            return new(expr);
        }
        else if (item is ExprAlias alias)
        {
            return new(alias);
        }
        else if (ExprRsolver.IsBindable(item))
        {
            return new(new BindValue(item));
        }
        else
        {
            throw new ArgumentException(
                $"Invalid type for SelectItem: {item.GetType()}");
        }
    }
}
