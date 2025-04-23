namespace InlineSqlSharp;

internal static class GroupByItemResolver
{
    internal static AbstractSqlPart[] Resolve(object[] groupByItems) =>
        groupByItems.Select(Resolve).ToArray();

    internal static AbstractSqlPart Resolve(object groupByItem)
    {
        if (groupByItem is AbstractExpr expr)
        {
            return expr;
        }
        else
        {
            throw new ArgumentException(
                $"Invalid type for GroupByItem: {groupByItem.GetType()}");
        }
    }
}
