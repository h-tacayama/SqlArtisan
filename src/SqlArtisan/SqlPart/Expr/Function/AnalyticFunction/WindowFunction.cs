namespace SqlArtisan;

public sealed class WindowFunction : AbstractExpr
{
    private readonly AbstractAnalyticFunction _analyticFunction;
    private readonly OverClause _overClause;

    internal WindowFunction(
        AbstractAnalyticFunction analyticFunction,
        OverClause overClause)
    {
        _analyticFunction = analyticFunction;
        _overClause = overClause;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_analyticFunction)
        .Append(_overClause);
}
