namespace SqlArtisan.Internal;

internal static class GroupByItemResolver
{
    internal static SqlPart[] Resolve(object[] groupByItems)
    {
        if (groupByItems is null)
        {
            throw new ArgumentNullException(
                nameof(groupByItems), ExpressionResolver.NullValueMessage);
        }

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
        else if (groupByItem is GroupingElement grouping)
        {
            return grouping;
        }
        else
        {
            throw new ArgumentException(
                $"Invalid type for GroupByItem: {groupByItem.GetType()}");
        }
    }

    // Resolves the inner elements of ROLLUP(...) / CUBE(...): each element is
    // either an ordinary grouping expression or a Sql.Group(...) composite column.
    internal static SqlPart[] ResolveElements(object[] elements)
    {
        var resolved = new SqlPart[elements.Length];

        for (int i = 0; i < elements.Length; i++)
        {
            resolved[i] = elements[i] is GroupingSet set
                ? set
                : ExpressionResolver.Resolve(elements[i]);
        }

        return resolved;
    }
}
