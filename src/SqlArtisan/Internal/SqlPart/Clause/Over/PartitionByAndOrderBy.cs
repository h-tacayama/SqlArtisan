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

    /// <summary>
    /// Adds a <c>RANGE bound</c> window frame to this partition/ordering (for use within <c>OVER</c>).
    /// </summary>
    public WindowFrameClause Range(FrameBound bound) =>
        new(this, new WindowFrame(Keywords.Range, bound));

    /// <summary>
    /// Adds a <c>RANGE BETWEEN start AND end</c> window frame to this partition/ordering
    /// (for use within <c>OVER</c>).
    /// </summary>
    public WindowFrameClause RangeBetween(FrameBound start, FrameBound end) =>
        new(this, new WindowFrame(Keywords.Range, new FrameBetween(start, end)));

    /// <summary>
    /// Adds a <c>ROWS bound</c> window frame to this partition/ordering (for use within <c>OVER</c>).
    /// </summary>
    public WindowFrameClause Rows(FrameBound bound) =>
        new(this, new WindowFrame(Keywords.Rows, bound));

    /// <summary>
    /// Adds a <c>ROWS BETWEEN start AND end</c> window frame to this partition/ordering
    /// (for use within <c>OVER</c>).
    /// </summary>
    public WindowFrameClause RowsBetween(FrameBound start, FrameBound end) =>
        new(this, new WindowFrame(Keywords.Rows, new FrameBetween(start, end)));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .AppendSpaceIfNotNull(_partitionByClause)
        .Append(_orderByClause);
}
