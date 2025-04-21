namespace InlineSqlSharp;

public sealed class WindowFunction(
    AbstractAnalyticFunction analyticFunction,
    OverClause overClause) : AbstractExpr
{
    private readonly AbstractAnalyticFunction _analyticFunction = analyticFunction;
    private readonly OverClause _overClause = overClause;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_analyticFunction)
        .Append(_overClause);
}
