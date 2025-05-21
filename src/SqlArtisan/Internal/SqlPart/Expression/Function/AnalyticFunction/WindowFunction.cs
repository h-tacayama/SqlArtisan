namespace SqlArtisan.Internal;

public sealed class WindowFunction : SqlExpression
{
    private readonly AnalyticFunction _analyticFunction;
    private readonly OverClause _overClause;

    internal WindowFunction(
        AnalyticFunction analyticFunction,
        OverClause overClause)
    {
        _analyticFunction = analyticFunction;
        _overClause = overClause;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_analyticFunction)
        .Append(_overClause);
}
