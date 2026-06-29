namespace SqlArtisan.Internal;

/// <summary>
/// An aggregate restricted to the rows matching a condition:
/// <c>SUM(x) FILTER (WHERE ...)</c>. Chain <c>.Over(...)</c> for the windowed form
/// <c>SUM(x) FILTER (WHERE ...) OVER (...)</c>.
/// </summary>
/// <remarks>
/// Native on PostgreSQL and SQLite. Per ADR 0001/0003 it is emitted faithfully on
/// every dialect; engines without it (and the analyzer) decide availability — the
/// library never rewrites it to a <c>CASE</c> expression.
/// </remarks>
public sealed class FilteredAggregateFunction : WindowableFunction
{
    private readonly AggregateFunction _aggregate;
    private readonly SqlCondition _condition;

    internal FilteredAggregateFunction(
        AggregateFunction aggregate,
        SqlCondition condition)
    {
        _aggregate = aggregate;
        _condition = condition;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_aggregate)
        .Append($" {Keywords.Filter} ")
        .OpenParenthesis(new WhereClause(_condition))
        .CloseParenthesis();
}
