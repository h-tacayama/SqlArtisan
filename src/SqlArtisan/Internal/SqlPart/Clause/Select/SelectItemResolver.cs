using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

internal static class SelectItemResolver
{
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
        else if (IsBindable(selectItem))
        {
            return new BindValue(selectItem);
        }
        else
        {
            throw new ArgumentException(
                $"Invalid type for SelectItem: {selectItem.GetType()}");
        }
    }
}
