namespace SqlArtisan.Internal;

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
    /// Turns the analytic function into a window function partitioned but not
    /// ordered: <c>OVER (PARTITION BY ...)</c>.
    /// </summary>
    public WindowFunction Over(PartitionByClause partitionByClause) =>
        new(this, OverClause.Of(partitionByClause));

    /// <summary>
    /// Turns the analytic function into a window function with an explicit frame:
    /// <c>OVER (... ROWS/RANGE ...)</c>.
    /// </summary>
    public WindowFunction Over(WindowFrameClause windowFrameClause) =>
        new(this, OverClause.Of(windowFrameClause));
}
