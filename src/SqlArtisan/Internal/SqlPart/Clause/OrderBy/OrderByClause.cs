namespace SqlArtisan.Internal;

public sealed class OrderByClause : SqlPart
{
    private readonly SqlPart[] _orderByItems;

    private OrderByClause(SqlPart[] orderByItems)
    {
        CollectionGuard.ThrowIfEmpty(
            orderByItems,
            "ORDER BY requires at least one item.");

        _orderByItems = orderByItems;
    }

    /// <summary>
    /// Adds a <c>RANGE bound</c> window frame to this ordering (for use within <c>OVER</c>).
    /// </summary>
    public WindowFrameClause Range(FrameBound bound) =>
        new(this, new WindowFrame(Keywords.Range, bound));

    /// <summary>
    /// Adds a <c>RANGE BETWEEN start AND end</c> window frame to this ordering
    /// (for use within <c>OVER</c>).
    /// </summary>
    public WindowFrameClause RangeBetween(FrameBound start, FrameBound end) =>
        new(this, new WindowFrame(Keywords.Range, new FrameBetween(start, end)));

    /// <summary>
    /// Adds a <c>ROWS bound</c> window frame to this ordering (for use within <c>OVER</c>).
    /// </summary>
    public WindowFrameClause Rows(FrameBound bound) =>
        new(this, new WindowFrame(Keywords.Rows, bound));

    /// <summary>
    /// Adds a <c>ROWS BETWEEN start AND end</c> window frame to this ordering
    /// (for use within <c>OVER</c>).
    /// </summary>
    public WindowFrameClause RowsBetween(FrameBound start, FrameBound end) =>
        new(this, new WindowFrame(Keywords.Rows, new FrameBetween(start, end)));

    internal static OrderByClause Parse(object[] orderByItems) =>
        new(OrderByItemResolver.Resolve(orderByItems));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Order} {Keywords.By} ")
        .AppendCsv(_orderByItems);
}
