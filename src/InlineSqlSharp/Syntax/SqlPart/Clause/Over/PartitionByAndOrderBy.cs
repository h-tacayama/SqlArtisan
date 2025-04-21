namespace InlineSqlSharp;

public sealed class PartitionByAndOrderBy(
    PartitionByClause partitionByClause,
    OrderByClause orderByClause) : AbstractSqlPart
{
    private readonly PartitionByClause _partitionByClause = partitionByClause;
    private readonly OrderByClause _orderByClause = orderByClause;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpaceIfNotNull(_partitionByClause)
        .Append(_orderByClause);
}
