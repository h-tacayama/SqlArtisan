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
    private readonly SqlCondition _condition;
    // Built once here, not per Format call, to keep the build path allocation-free
    // (ADR 0006).
    private readonly WhereClause _filterWhere;

    internal FilteredAggregateFunction(
        UnfilteredAggregateFunction aggregate,
        SqlCondition condition)
    {
        _aggregate = aggregate;
        _condition = condition;
        _filterWhere = new WhereClause(condition);
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        // Guarded on _condition here, ahead of the embedded _filterWhere (itself a
        // WhereClause that would also throw), so the message names FILTER, not WHERE.
        ConditionGuard.ThrowIfEmpty(
            _condition,
            "An aggregate's FILTER requires a condition; omit it for an unfiltered aggregate.");

        buffer
            .Append(_aggregate)
            .EncloseInSpaces(Keywords.Filter)
            .OpenParenthesis(_filterWhere)
            .CloseParenthesis();
    }
}
