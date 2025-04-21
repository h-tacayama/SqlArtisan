namespace InlineSqlSharp;

internal sealed class GroupByClause : AbstractSqlPart
{
    private readonly GroupByItem[] _items;

    private GroupByClause(GroupByItem[] items)
    {
        _items = items;
    }

    internal static GroupByClause Parse(object[] items) =>
        new(GroupByItemResolver.Resolve(items));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.GROUP)
        .AppendSpace(Keywords.BY)
        .AppendCsv(_items);
}
