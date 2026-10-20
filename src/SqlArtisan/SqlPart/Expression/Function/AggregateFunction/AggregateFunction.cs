using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Base class for the aggregate functions (e.g. <c>SUM</c>), each of which can become a
/// window function via <c>Over(...)</c>; a row filter is added with
/// <see cref="UnfilteredAggregateFunction.Filter(SqlCondition)"/>.
/// </summary>
public abstract class AggregateFunction : SqlExpression
{
    private protected AggregateFunction()
    {
    }

    /// <summary>
    /// Turns it into a window function over the whole result set: <c>OVER ()</c>.
    /// </summary>
    public SqlExpression Over() =>
        new WindowFunction(this, OverClause.Of());

    /// <summary>
    /// Turns it into a window function partitioned by the given expressions:
    /// <c>OVER (PARTITION BY ...)</c>.
    /// </summary>
    public SqlExpression Over(PartitionByClause partitionByClause) =>
        new WindowFunction(this, OverClause.Of(partitionByClause));

    /// <summary>
    /// Turns it into a window function ordered over the whole result set:
    /// <c>OVER (ORDER BY ...)</c>.
    /// </summary>
    public SqlExpression Over(OrderByClause orderByClause) =>
        new WindowFunction(this, OverClause.Of(orderByClause));

    /// <summary>
    /// Turns it into a window function partitioned and ordered:
    /// <c>OVER (PARTITION BY ... ORDER BY ...)</c>.
    /// </summary>
    public SqlExpression Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new WindowFunction(this, OverClause.Of(partitionByAndOrderBy));

    /// <summary>
    /// Turns it into a window function with an explicit frame:
    /// <c>OVER (... ROWS/RANGE ...)</c>.
    /// </summary>
    public SqlExpression Over(WindowFrameClause windowFrameClause) =>
        new WindowFunction(this, OverClause.Of(windowFrameClause));
}
