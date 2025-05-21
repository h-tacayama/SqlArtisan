namespace SqlArtisan.Internal;

internal static class GroupByItemResolver
{
    internal static SqlPart[] Resolve(object[] groupByItems)
    {
        var resolved = new SqlPart[groupByItems.Length];

        for (int i = 0; i < groupByItems.Length; i++)
        {
            resolved[i] = Resolve(groupByItems[i]);
        }

        return resolved;
    }

    internal static SqlPart Resolve(object groupByItem)
    {
        if (groupByItem is SqlExpression expr)
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
