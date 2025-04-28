namespace InlineSqlSharp;

internal sealed class GroupByClause : AbstractSqlPart
{
    private readonly AbstractSqlPart[] _groupByItems;

    private GroupByClause(AbstractSqlPart[] groupByItems)
    {
        _groupByItems = groupByItems;
    }

    internal static GroupByClause Parse(object[] groupByItems) =>
        new(GroupByItemResolver.Resolve(groupByItems));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Group)
        .AppendSpace(Keywords.By)
        .AppendCsv(_groupByItems);
}
