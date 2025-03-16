namespace InlineSqlSharp;

public sealed class GreaterThanOrEqualCondition(
	IExpr leftSide,
	IExpr rightSide) : IComparisonCondition
{
	private readonly ComparisonConditionCore _core =
		new(leftSide, Operators.GreaterThanOrEqual, rightSide);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
