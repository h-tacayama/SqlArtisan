namespace InlineSqlSharp;

public sealed class PartitionByClause : AbstractSqlPart
{
    private readonly AbstractExpr[] _expressions;

    internal PartitionByClause(AbstractExpr[] expressions)
    {
        _expressions = expressions;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.PARTITION)
        .AppendSpace(Keywords.BY)
        .AppendCsv(_expressions);

    public PartitionByAndOrderBy ORDER_BY(
        params object[] orderByItems) =>
        new(this, OrderByClause.Parse(orderByItems));
}
