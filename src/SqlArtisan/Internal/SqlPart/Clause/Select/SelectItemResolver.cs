using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

internal static class SelectItemResolver
{
    // The eager guard for the SELECT list (the #236 empty-state policy): a SELECT
    // list can never legally be empty, and the count is fixed at the call site, so
    // it throws immediately rather than at Build(). RETURNING resolves through the
    // plain Resolve below (it guards its own emptiness in ReturningBuilder.Create).
    internal static SqlPart[] ResolveOrThrow(object[] selectItems)
    {
        CollectionGuard.ThrowIfEmpty(selectItems, "SELECT requires at least one item.");
        return Resolve(selectItems);
    }

    internal static SqlPart[] Resolve(object[] selectItems)
    {
        var resolved = new SqlPart[selectItems.Length];

        for (int i = 0; i < selectItems.Length; i++)
        {
            resolved[i] = Resolve(selectItems[i]);
        }

        return resolved;
    }

    internal static SqlPart Resolve(object selectItem)
    {
        if (selectItem is SqlExpression expr)
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
        else if (IsBindable(selectItem))
        {
            return new BindValue(selectItem);
        }
        else
        {
            throw UnresolvableValue("SelectItem", selectItem);
        }
    }
}
