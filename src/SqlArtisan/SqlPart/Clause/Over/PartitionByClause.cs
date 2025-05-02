namespace SqlArtisan;

public sealed class PartitionByClause : SqlPart
{
    private readonly SqlExpression[] _expressions;

    internal PartitionByClause(SqlExpression[] expressions)
    {
        _expressions = expressions;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Partition} {Keywords.By} ")
        .AppendCsv(_expressions);

    public PartitionByAndOrderBy OrderBy(
        params object[] orderByItems) =>
        new(this, OrderByClause.Parse(orderByItems));
}
