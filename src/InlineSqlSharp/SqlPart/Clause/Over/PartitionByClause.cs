namespace InlineSqlSharp;

public sealed class PartitionByClause : AbstractSqlPart
{
    private readonly AbstractExpr[] _expressions;

    internal PartitionByClause(AbstractExpr[] expressions)
    {
        _expressions = expressions;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Partition)
        .AppendSpace(Keywords.By)
        .AppendCsv(_expressions);

    public PartitionByAndOrderBy OrderBy(
        params object[] orderByItems) =>
        new(this, OrderByClause.Parse(orderByItems));
}
