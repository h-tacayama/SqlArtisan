namespace InlineSqlSharp;

public sealed class PartitionByClause(AbstractExpr[] expressions) :
    AbstractSqlPart
{
    private readonly AbstractExpr[] _expressions = expressions;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.PARTITION)
        .AppendSpace(Keywords.BY)
        .AppendCsv(_expressions);

    public PartitionByAndOrderBy ORDER_BY(
        params object[] orderByItems) =>
        new(this, OrderByClause.Parse(orderByItems));
}
