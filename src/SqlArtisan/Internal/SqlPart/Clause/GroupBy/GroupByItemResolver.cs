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
            throw ExpressionResolver.UnresolvableValue("GroupByItem", groupByItem);
        }
    }

    // The leading element is split from the `params` tail so a null tail array —
    // the C# binding for e.g. Rollup(a, null) — throws a named exception here
    // instead of an NRE when spread into a collection expression.
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
