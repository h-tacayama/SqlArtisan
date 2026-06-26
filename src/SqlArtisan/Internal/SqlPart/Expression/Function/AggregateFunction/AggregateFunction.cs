namespace SqlArtisan.Internal;

/// <summary>
/// Base class for aggregate functions (<c>SUM</c>, <c>COUNT</c>, <c>AVG</c>,
/// <c>MAX</c>, <c>MIN</c>) that can also be used as window functions via
/// <c>Over(...)</c>.
/// </summary>
/// <remarks>
/// Note: most databases do not support <c>DISTINCT</c> in a windowed aggregate
/// (e.g. <c>SUM(DISTINCT x) OVER (...)</c>), so combining a distinct aggregate
/// with <c>Over(...)</c> generates SQL that the database will reject.
/// </remarks>
public abstract class AggregateFunction : SqlExpression
{
    /// <summary>
    /// Turns the aggregate into a window function over the whole result set:
    /// <c>OVER ()</c>.
    /// </summary>
    public WindowFunction Over() =>
        new(this, OverClause.Of());

    /// <summary>
    /// Turns the aggregate into a window function partitioned by the given
    /// expressions: <c>OVER (PARTITION BY ...)</c>.
    /// </summary>
    public WindowFunction Over(PartitionByClause partitionByClause) =>
        new(this, OverClause.Of(partitionByClause));

    /// <summary>
    /// Turns the aggregate into a window function ordered over the whole result
    /// set: <c>OVER (ORDER BY ...)</c>.
    /// </summary>
    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    /// <summary>
    /// Turns the aggregate into a window function partitioned and ordered:
    /// <c>OVER (PARTITION BY ... ORDER BY ...)</c>.
    /// </summary>
    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    /// <summary>
    /// Turns the aggregate into a window function with an explicit frame:
    /// <c>OVER (... ROWS/RANGE ...)</c>.
    /// </summary>
    public WindowFunction Over(WindowFrameClause windowFrameClause) =>
        new(this, OverClause.Of(windowFrameClause));
}
