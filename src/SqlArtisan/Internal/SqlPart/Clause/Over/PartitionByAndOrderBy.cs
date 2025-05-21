namespace SqlArtisan.Internal;

public sealed class PartitionByAndOrderBy : SqlPart
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

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .AppendSpaceIfNotNull(_partitionByClause)
        .Append(_orderByClause);
}
