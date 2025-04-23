namespace InlineSqlSharp;

public sealed class OrderByClause : AbstractSqlPart
{
    private readonly AbstractSqlPart[] _orderByItems;

    private OrderByClause(AbstractSqlPart[] orderByItems)
    {
        _orderByItems = orderByItems;
    }

    internal static OrderByClause Parse(object[] orderByItems) =>
        new(OrderByItemResolver.Resolve(orderByItems));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.ORDER)
        .AppendSpace(Keywords.BY)
        .AppendCsv(_orderByItems);
}
