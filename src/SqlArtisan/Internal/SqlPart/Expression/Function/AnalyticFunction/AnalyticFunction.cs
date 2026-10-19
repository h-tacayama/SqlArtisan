namespace SqlArtisan.Internal;

/// <summary>
/// Base class for the ranking and offset (<c>LAG</c>/<c>LEAD</c>) analytic
/// functions, offered an ordered window and no frame.
/// </summary>
/// <remarks>
/// <para>Only the ordered <c>Over(...)</c> overloads are declared here — the unordered
/// and framed ones belong to <see cref="ValueAnalyticFunction"/> — and this is no
/// <see cref="SqlExpression"/>: only a completed <c>Over(...)</c> call yields one.</para>
/// <para>PostgreSQL 16.15 and SQLite 3.45.1 run an unordered window here and Oracle does
/// not, but <c>OrderBy(Null)</c> reads the same rows on the first two, so withholding it
/// is ergonomic, not a hole (<c>public-api-design.md</c> § "Opinions…", #664).</para>
/// </remarks>
public abstract class AnalyticFunction : SqlPart, IIncompleteExpression
{
    private protected AnalyticFunction()
    {
    }

    string IIncompleteExpression.IncompleteMessage =>
        $"{FunctionName} requires an OVER clause; complete it with .Over(...).";

    private protected abstract string FunctionName { get; }

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
