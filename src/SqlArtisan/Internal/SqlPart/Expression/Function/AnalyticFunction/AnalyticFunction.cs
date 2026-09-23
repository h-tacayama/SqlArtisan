namespace SqlArtisan.Internal;

/// <summary>
/// Base class for the ranking and offset (<c>LAG</c>/<c>LEAD</c>) analytic
/// functions, whose window must be ordered and cannot take a frame.
/// </summary>
/// <remarks>
/// Only the ordered <c>Over(...)</c> overloads are declared here — the unordered
/// and framed ones belong to <see cref="ValueAnalyticFunction"/> — and this is no
/// <see cref="SqlExpression"/>: only a completed <c>Over(...)</c> call yields one.
/// </remarks>
public abstract class AnalyticFunction : SqlPart, IIncompleteExpression
{
    private protected AnalyticFunction()
    {
    }

    string IIncompleteExpression.CompletionHint =>
        "Complete it with .Over(...) — a window function requires an OVER clause.";

    /// <summary>
    /// Turns the analytic function into a window function ordered over the whole
    /// result set: <c>OVER (ORDER BY ...)</c>.
    /// </summary>
    public WindowFunction Over(OrderByClause orderByClause) =>
        new(this, OverClause.Of(orderByClause));

    /// <summary>
    /// Turns the analytic function into a window function partitioned and
    /// ordered: <c>OVER (PARTITION BY ... ORDER BY ...)</c>.
    /// </summary>
    public WindowFunction Over(PartitionByAndOrderBy partitionByAndOrderBy) =>
        new(this, OverClause.Of(partitionByAndOrderBy));
}
