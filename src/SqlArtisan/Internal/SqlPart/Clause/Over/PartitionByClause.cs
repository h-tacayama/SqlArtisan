namespace SqlArtisan.Internal;

public sealed class PartitionByClause : SqlPart
{
    private readonly SqlExpression[] _expressions;

    internal PartitionByClause(SqlExpression[] expressions)
    {
        if (expressions.Length == 0)
        {
            throw new ArgumentException(
                "PARTITION BY requires at least one expression.");
        }

        _expressions = expressions;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Partition} {Keywords.By} ")
        .AppendCsv(_expressions);

    public PartitionByAndOrderBy OrderBy(
        params object[] orderByItems) =>
        new(this, OrderByClause.Parse(orderByItems));
}
