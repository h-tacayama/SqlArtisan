namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static OrCondition OR(params ICondition[] conditions) =>
        new(conditions);

    public static OrderByClause ORDER_BY(
        params IExprOrAliasOrSortOrder[] sortExpressions) =>
        new OrderByClause(sortExpressions);
}
