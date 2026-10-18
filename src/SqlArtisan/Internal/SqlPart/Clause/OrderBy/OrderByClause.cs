namespace SqlArtisan.Internal;

public sealed class OrderByClause : SqlPart
{
    private readonly SqlPart[] _orderByItems;

    private OrderByClause(SqlPart[] orderByItems)
    {
        CollectionGuard.ThrowIfEmpty(
            orderByItems, nameof(orderByItems), "ORDER BY requires at least one item.");

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

    // PostgreSQL and SQLite read no select alias here, so its bare name sorts by an
    // input column or a constant; the aliased expression reads the same everywhere (#672).
    // Nor a select-list position: a number is a constant key, as in PARTITION BY (#693).
    internal static OrderByClause ParseInExpression(object[] orderByItems)
    {
        ThrowIfNumericKey(orderByItems);
        SqlPart[] items = OrderByItemResolver.Resolve(orderByItems);

        foreach (SqlPart item in items)
        {
            if (item is ExpressionAlias or SortOrder { SortsByAlias: true })
            {
                throw new ArgumentException(
                    "An ORDER BY inside OVER, WITHIN GROUP or an aggregate cannot sort by a "
                        + "select alias; pass the aliased expression instead.",
                    nameof(orderByItems));
            }
        }

        return new(items);
    }

    // Checked before resolving, so NaN gets this message rather than the finite one;
    // a null array or element is left to the resolver's own message.
    private static void ThrowIfNumericKey(object[] orderByItems)
    {
        if (orderByItems is null)
        {
            return;
        }

        foreach (object item in orderByItems)
        {
            if (item is not null && ExpressionResolver.IsNumeric(item))
            {
                throw new ArgumentException(
                    "An ORDER BY inside OVER, WITHIN GROUP or an aggregate cannot sort by a "
                        + "number, which is a constant there; pass a column or an expression, "
                        + "or Bind(value) for a deliberate constant.",
                    nameof(orderByItems));
            }
        }
    }

    // Read by the PostgreSQL bounded-exception guard (ADR 0011).
    internal bool HasFractionalSortKey
    {
        get
        {
            foreach (SqlPart item in _orderByItems)
            {
                if (item is NumericSortKey { IsFractional: true })
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Read by SelectBuilder.OrderBy's dialect-blind guard (ADR 0007): only the
    // statement position resolves an ordinal.
    internal bool HasZeroOrdinal
    {
        get
        {
            foreach (SqlPart item in _orderByItems)
            {
                if (item is NumericSortKey { IsZeroOrdinal: true })
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Read by the PostgreSQL/SQLite bounded-exception guard (ADR 0011): MySQL
    // reads a negative literal as a constant and accepts it.
    internal bool HasNegativeOrdinal
    {
        get
        {
            foreach (SqlPart item in _orderByItems)
            {
                if (item is NumericSortKey { IsNegativeOrdinal: true })
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Order} {Keywords.By} ")
        .AppendCsv(_orderByItems);
}
