namespace SqlArtisan.Internal;

public sealed class OverClause : SqlPart
{
    private readonly PartitionByClause? _partitionByClause;
    private readonly OrderByClause? _orderByClause;
    private readonly PartitionByAndOrderBy? _partitionByAndOrderBy;

    private OverClause(
        PartitionByClause? partitionByClause = null,
        PartitionByAndOrderBy? partitionByAndOrderBy = null,
        OrderByClause? orderByClause = null)
    {
        _partitionByClause = partitionByClause;
        _partitionByAndOrderBy = partitionByAndOrderBy;
        _orderByClause = orderByClause;
    }

    internal static OverClause Of() => new();

    internal static OverClause Of(PartitionByClause partitionByClause) =>
        new(partitionByClause);

    internal static OverClause Of(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(null, partitionByAndOrderBy);

    internal static OverClause Of(OrderByClause orderByClause) =>
        new(null, null, orderByClause);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Over);

        if (_partitionByClause is null
            && _partitionByAndOrderBy is null
            && _orderByClause is null)
        {
            buffer.OpenParenthesis()
                .CloseParenthesis();
            return;
        }

        buffer.AppendSpace()
            .OpenParenthesis();

        if (_partitionByAndOrderBy is not null)
        {
            _partitionByAndOrderBy.Format(buffer);
        }
        else if (_partitionByClause is not null)
        {
            _partitionByClause.Format(buffer);
        }
        else if (_orderByClause is not null)
        {
            _orderByClause.Format(buffer);
        }

        buffer.CloseParenthesis();
    }
}
