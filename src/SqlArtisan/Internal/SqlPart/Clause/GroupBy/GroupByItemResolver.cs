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
        if (groupByItem is null)
        {
            throw new ArgumentNullException(
                nameof(groupByItem), ExpressionResolver.NullValueMessage);
        }

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

    // Resolves the elements of ROLLUP(...) / CUBE(...): a required leading element
    // plus zero or more trailing ones, each an ordinary grouping expression or a
    // Sql.Group(...) composite column. The leading element is taken separately so
    // the factory can pass its `params` array straight through — a null array (the
    // C# binding for e.g. Rollup(a, null)) then throws a clear ArgumentNullException
    // instead of failing with an NRE when spread into a collection expression.
    internal static SqlPart[] ResolveElements(object element, params object[] elements)
    {
        if (elements is null)
        {
            throw new ArgumentNullException(
                nameof(elements), ExpressionResolver.NullValueMessage);
        }

        SqlPart[] resolved = new SqlPart[elements.Length + 1];
        resolved[0] = ResolveElement(element);

        for (int i = 0; i < elements.Length; i++)
        {
            resolved[i + 1] = ResolveElement(elements[i]);
        }

        return resolved;
    }

    private static SqlPart ResolveElement(object element) =>
        element is GroupingSet set ? set : ExpressionResolver.Resolve(element);
}
