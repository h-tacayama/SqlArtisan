namespace InlineSqlSharp;

internal static class SelectItemResolver
{
    internal static AbstractSqlPart[] Resolve(object[] selectItems) =>
        selectItems.Select(Resolve).ToArray();

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
