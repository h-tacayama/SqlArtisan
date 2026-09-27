using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

internal static class SelectItemResolver
{
    // The eager guard for the SELECT list (the #236 empty-state policy): a SELECT
    // list can never legally be empty, so it throws at the call rather than at
    // Build(). RETURNING guards its own emptiness in ReturningBuilder.Create.
    internal static SqlPart[] ResolveOrThrow(object[] selectItems)
    {
        CollectionGuard.ThrowIfEmpty(
            selectItems, nameof(selectItems), "SELECT requires at least one item.");
        return Resolve(selectItems, "SelectItem", "selectItem");
    }

    // RETURNING and OUTPUT share the SELECT list's grammar but name their own
    // position in a failure, since the value never reached a SELECT list.
    internal static SqlPart[] Resolve(object[] items, string position, string paramName)
    {
        var resolved = new SqlPart[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            resolved[i] = Resolve(items[i], position, paramName);
        }

        return resolved;
    }

    private static SqlPart Resolve(object selectItem, string position, string paramName)
    {
        if (selectItem is null)
        {
            throw new ArgumentNullException(paramName, ExpressionResolver.NullValueMessage);
        }
        else if (selectItem is SqlExpression expr)
        {
            return expr;
        }
        else if (selectItem is ExpressionAlias alias)
        {
            return alias;
        }
        else if (selectItem is ISubquery subquery)
        {
            return new ScalarSubquery(subquery);
        }
        else if (selectItem is AsteriskMarker or QualifiedAsteriskMarker)
        {
            return (SqlPart)selectItem;
        }
        else if (IsBindable(selectItem))
        {
            return new BindValue(selectItem);
        }
        else
        {
            throw UnresolvableValue(position, selectItem);
        }
    }
}
