namespace InlineSqlSharp;

public sealed class GreaterThanCondition(
	IExpr leftSide,
	IExpr rightSide) : IComparisonCondition
{
	private readonly ComparisonConditionCore _core =
		new(leftSide, Operators.GreaterThan, rightSide);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
