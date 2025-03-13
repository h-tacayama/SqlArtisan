namespace InlineSqlSharp;

public sealed class WindowFunction(
		AnalyticFunction analyticFunction,
		OverClause overClause) : NumericExpr
{
	private readonly AnalyticFunction _analyticFunction = analyticFunction;
	private readonly OverClause _overClause = overClause;

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(_analyticFunction)
		.PrependLine(_overClause);
}
