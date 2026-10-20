namespace SqlArtisan.Internal;

/// <summary>
/// A <c>DISTINCT</c> aggregate restricted to the rows matching a condition:
/// <c>COUNT(DISTINCT x) FILTER (WHERE ...)</c>.
/// </summary>
/// <remarks>
/// It offers no window: PostgreSQL 16/17 and SQLite 3.50, the engines with <c>FILTER</c>, run
/// no <c>DISTINCT</c> window (live twins, #662).
/// </remarks>
internal sealed class FilteredDistinctAggregateFunction : SqlExpression
{
    private readonly DistinctAggregateFunction _aggregate;
    private readonly SqlCondition _condition;
    private readonly WhereClause _filterWhere;

    internal FilteredDistinctAggregateFunction(
        DistinctAggregateFunction aggregate,
        SqlCondition condition)
    {
        _aggregate = aggregate;
        _condition = condition;
        _filterWhere = new WhereClause(condition);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        FilteredAggregateFunction.FormatFiltered(buffer, _aggregate, _condition, _filterWhere);
}
