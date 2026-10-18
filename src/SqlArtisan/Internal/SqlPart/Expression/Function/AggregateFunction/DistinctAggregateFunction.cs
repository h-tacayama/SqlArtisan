namespace SqlArtisan.Internal;

/// <summary>
/// A <c>DISTINCT</c> aggregate. It offers only the windows and the filter an engine runs on
/// one.
/// </summary>
/// <remarks>
/// Its empty and partition-only windows run on Oracle XE 21.3.0 and Free 23ai alone. MySQL 8.0,
/// PostgreSQL 16/17, SQLite 3.50 and SQL Server 2022/2025 reject every window on one, and those
/// Oracle lanes reject an ordered or framed one, so it does not compile (live twins, #662).
/// </remarks>
public sealed class DistinctAggregateFunction : SqlExpression
{
    private readonly string _name;
    private readonly DistinctKeyword _distinct;
    private readonly SqlExpression _expr;

    internal DistinctAggregateFunction(string name, DistinctKeyword distinct, SqlExpression expr)
    {
        ArgumentNullException.ThrowIfNull(distinct);

        _name = name;
        _distinct = distinct;
        _expr = expr;
    }

    /// <summary>
    /// Restricts the aggregate to rows matching <paramref name="condition"/>:
    /// <c>COUNT(DISTINCT x) FILTER (WHERE ...)</c>. Native on PostgreSQL and SQLite;
    /// emitted faithfully elsewhere for the database to accept or reject.
    /// </summary>
    /// <param name="condition">The <c>WHERE</c> condition the aggregate counts.</param>
    /// <returns>The aggregate restricted by <c>FILTER (WHERE ...)</c>.</returns>
    public FilteredDistinctAggregateFunction Filter(SqlCondition condition) =>
        new(this, condition);

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

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_name)
        .OpenParenthesis()
        .AppendSpace(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
