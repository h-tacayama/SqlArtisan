namespace InlineSqlSharp;

internal static class GroupByItemResolver
{
    internal static GroupByItem[] Resolve(object[] items) =>
        items.Select(Resolve).ToArray();

    internal static GroupByItem Resolve(object item)
    {
        if (item is AbstractExpr expr)
        {
            return new(expr);
        }
        else
        {
            throw new ArgumentException(
                $"Invalid type for GroupByItem: {item.GetType()}");
        }
    }
}
