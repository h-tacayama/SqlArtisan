namespace InlineSqlSharp;

public sealed class OrderByClause : AbstractSqlPart
{
    private readonly OrderByItem[] _items;

    private OrderByClause(OrderByItem[] items)
    {
        _items = items;
    }

    internal static OrderByClause Parse(object[] items) =>
        new(OrderByItemResolver.Resolve(items));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.ORDER)
        .AppendSpace(Keywords.BY)
        .AppendCsv(_items);
}
