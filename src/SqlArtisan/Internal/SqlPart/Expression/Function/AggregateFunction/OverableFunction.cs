namespace SqlArtisan.Internal;

/// <summary>
/// Base class for an expression that takes an <c>OVER (...)</c> clause to become a
/// window function — an <see cref="AggregateFunction"/> or a
/// <see cref="FilteredAggregateFunction"/>.
/// </summary>
public abstract class OverableFunction : SqlExpression
{
    /// <summary>
    /// Turns it into a window function over the whole result set: <c>OVER ()</c>.
    /// </summary>
    public WindowFunction Over() =>
        new(this, OverClause.Of());

    /// <summary>
    /// Turns it into a window function partitioned by the given expressions:
    /// <c>OVER (PARTITION BY ...)</c>.
    /// </summary>
    public WindowFunction Over(PartitionByClause partitionByClause) =>
        new(this, OverClause.Of(partitionByClause));

    /// <summary>
    /// Turns it into a window function ordered over the whole result set:
    /// <c>OVER (ORDER BY ...)</c>.
    /// </summary>
    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    /// <summary>
    /// Turns it into a window function partitioned and ordered:
    /// <c>OVER (PARTITION BY ... ORDER BY ...)</c>.
    /// </summary>
    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    /// <summary>
    /// Turns it into a window function with an explicit frame:
    /// <c>OVER (... ROWS/RANGE ...)</c>.
    /// </summary>
    public WindowFunction Over(WindowFrameClause windowFrameClause) =>
        new(this, OverClause.Of(windowFrameClause));
}
