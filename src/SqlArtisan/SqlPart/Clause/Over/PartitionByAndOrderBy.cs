namespace InlineSqlSharp;

public sealed class PartitionByAndOrderBy : AbstractSqlPart
{
    private readonly PartitionByClause _partitionByClause;
    private readonly OrderByClause _orderByClause;

    internal PartitionByAndOrderBy(
        PartitionByClause partitionByClause,
        OrderByClause orderByClause)
    {
        _partitionByClause = partitionByClause;
        _orderByClause = orderByClause;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpaceIfNotNull(_partitionByClause)
        .Append(_orderByClause);
}
