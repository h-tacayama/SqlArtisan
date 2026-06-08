namespace SqlArtisan.Internal;

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
    /// Turns the aggregate into a window function partitioned and ordered:
    /// <c>OVER (PARTITION BY ... ORDER BY ...)</c>.
    /// </summary>
    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));

    /// <summary>
    /// Turns the aggregate into a window function ordered over the whole result
    /// set: <c>OVER (ORDER BY ...)</c>.
    /// </summary>
    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));
}
