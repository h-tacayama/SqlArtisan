namespace InlineSqlSharp;

internal static class GroupByItemResolver
{
    internal static AbstractSqlPart[] Resolve(object[] groupByItems)
    {
        var resolved = new AbstractSqlPart[groupByItems.Length];

        for (int i = 0; i < groupByItems.Length; i++)
        {
            resolved[i] = Resolve(groupByItems[i]);
        }

        return resolved;
    }

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
