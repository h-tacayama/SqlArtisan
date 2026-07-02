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

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_aggregate)
        .EncloseInSpaces(Keywords.Filter)
        .OpenParenthesis(_filterWhere)
        .CloseParenthesis();
}
