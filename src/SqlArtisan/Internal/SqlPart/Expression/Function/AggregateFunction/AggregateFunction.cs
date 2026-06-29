namespace SqlArtisan.Internal;

/// <summary>
/// Base class for aggregate functions (<c>SUM</c>, <c>COUNT</c>, <c>AVG</c>,
/// <c>MAX</c>, <c>MIN</c>) that can also be used as window functions via
/// <c>Over(...)</c> or restricted to matching rows via <c>Filter(...)</c>.
/// </summary>
/// <remarks>
/// Note: most databases do not support <c>DISTINCT</c> in a windowed aggregate
/// (e.g. <c>SUM(DISTINCT x) OVER (...)</c>), so combining a distinct aggregate
/// with <c>Over(...)</c> generates SQL that the database will reject.
/// </remarks>
public abstract class AggregateFunction : WindowableFunction
{
    /// <summary>
    /// Restricts the aggregate to rows matching <paramref name="condition"/>:
    /// <c>SUM(x) FILTER (WHERE ...)</c>. Chain <c>.Over(...)</c> afterwards for a
    /// filtered window function. Native on PostgreSQL and SQLite; emitted
    /// faithfully elsewhere for the database to accept or reject.
    /// </summary>
    /// <param name="condition">The <c>WHERE</c> condition the aggregate counts.</param>
    /// <returns>The aggregate restricted by <c>FILTER (WHERE ...)</c>.</returns>
    public FilteredAggregateFunction Filter(SqlCondition condition) =>
        new(this, condition);
}
