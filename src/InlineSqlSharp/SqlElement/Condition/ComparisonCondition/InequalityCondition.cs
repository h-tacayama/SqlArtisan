namespace InlineSqlSharp;

public sealed class InequalityCondition(
	IExpr leftSide,
	IExpr rightSide) : IEqualityCondition
{
	private readonly ComparisonConditionCore _core =
		new(leftSide, Operators.Inequality, rightSide);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
