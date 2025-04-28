namespace InlineSqlSharp;
public static partial class SqlWordbook
{
    public static OrCondition Or(params AbstractCondition[] conditions) =>
        new(conditions);

    public static OrderByClause OrderBy(
        params object[] orderByItems) =>
        OrderByClause.Parse(orderByItems);
}
