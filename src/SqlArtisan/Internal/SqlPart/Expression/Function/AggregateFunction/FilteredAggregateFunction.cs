namespace SqlArtisan.Internal;

/// <summary>
/// An aggregate restricted to the rows matching a condition:
/// <c>SUM(x) FILTER (WHERE ...)</c>. Chain <c>.Over(...)</c> for the windowed form
/// <c>SUM(x) FILTER (WHERE ...) OVER (...)</c>.
/// </summary>
/// <remarks>
/// Native on PostgreSQL and SQLite; emitted as written on every dialect (engines
/// without it reject it).
/// </remarks>
public sealed class FilteredAggregateFunction : AggregateFunction
{
    private readonly UnfilteredAggregateFunction _aggregate;
    // Built once here, not per Format call, to keep the build path allocation-free
    // (ADR 0006).
    private readonly WhereClause _filterWhere;

    internal FilteredAggregateFunction(
        UnfilteredAggregateFunction aggregate,
        SqlCondition condition)
    {
        _aggregate = aggregate;
        _filterWhere = new WhereClause(condition);
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        // An all-empty filter condition drops the whole FILTER (WHERE ...) wrapper
        // — an unfiltered aggregate is just the aggregate (the #236 empty-state
        // policy) — rather than emitting FILTER (WHERE ) (invalid on every dialect).
        if (_filterWhere.IsEmpty)
        {
            buffer.Append(_aggregate);
            return;
        }

        buffer
            .Append(_aggregate)
            .EncloseInSpaces(Keywords.Filter)
            .OpenParenthesis(_filterWhere)
            .CloseParenthesis();
    }
}
