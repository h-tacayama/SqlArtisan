namespace InlineSqlSharp;
public static partial class SqlWordbook
{
    public static OrCondition OR(params AbstractCondition[] conditions) =>
        new(conditions);

    public static OrderByClause ORDER_BY(
        params object[] orderByItems) =>
        OrderByClause.Parse(orderByItems);
}
