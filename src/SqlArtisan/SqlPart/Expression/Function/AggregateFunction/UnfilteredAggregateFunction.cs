using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Base class for a plain aggregate (e.g. <c>SUM</c>) — one with no row filter
/// yet, so it can be restricted to matching rows via <c>Filter(...)</c> (and
/// windowed via <c>Over(...)</c>).
/// </summary>
public abstract class UnfilteredAggregateFunction : AggregateFunction
{
    private protected UnfilteredAggregateFunction()
    {
    }

    /// <summary>
    /// Restricts the aggregate to rows matching <paramref name="condition"/>:
    /// <c>SUM(x) FILTER (WHERE ...)</c>; chain <c>.Over(...)</c> for a filtered window.
    /// Native on PostgreSQL and SQLite; emitted faithfully elsewhere.
    /// </summary>
    /// <param name="condition">The <c>WHERE</c> condition the aggregate counts.</param>
    /// <returns>The aggregate restricted by <c>FILTER (WHERE ...)</c>.</returns>
    public AggregateFunction Filter(SqlCondition condition) =>
        new FilteredAggregateFunction(this, condition);
}
