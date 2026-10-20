using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Base class for a value analytic function, whose window may be left unordered
/// or extended with an explicit frame.
/// </summary>
public abstract class ValueAnalyticFunction : AnalyticFunction
{
    private protected ValueAnalyticFunction()
    {
    }

    /// <summary>
    /// Turns the analytic function into a window function over the whole result
    /// set: <c>OVER ()</c>.
    /// </summary>
    public SqlExpression Over() =>
        new WindowFunction(this, OverClause.Of());

    /// <summary>
    /// Turns the analytic function into a window function partitioned but not
    /// ordered: <c>OVER (PARTITION BY ...)</c>.
    /// </summary>
    public SqlExpression Over(PartitionByClause partitionByClause) =>
        new WindowFunction(this, OverClause.Of(partitionByClause));

    /// <summary>
    /// Turns the analytic function into a window function with an explicit frame:
    /// <c>OVER (... ROWS/RANGE ...)</c>.
    /// </summary>
    public SqlExpression Over(WindowFrameClause windowFrameClause) =>
        new WindowFunction(this, OverClause.Of(windowFrameClause));
}
