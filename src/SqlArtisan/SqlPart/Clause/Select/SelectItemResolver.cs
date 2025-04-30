namespace SqlArtisan;

internal static class SelectItemResolver
{
    internal static AbstractSqlPart[] Resolve(object[] selectItems)
    {
        var resolved = new AbstractSqlPart[selectItems.Length];

        for (int i = 0; i < selectItems.Length; i++)
        {
            resolved[i] = Resolve(selectItems[i]);
        }

        return resolved;
    }

    internal static AbstractSqlPart Resolve(object selectItem)
    {
        if (selectItem is AbstractExpr expr)
        {
            return expr;
        }
        else if (selectItem is ExprAlias alias)
        {
            return alias;
        }
        else if (ExprResolver.IsBindable(selectItem))
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
