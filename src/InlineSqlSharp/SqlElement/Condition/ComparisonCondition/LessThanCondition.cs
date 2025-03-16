namespace InlineSqlSharp;

public sealed class LessThanCondition(
	IExpr leftSide,
	IExpr rightSide) : IComparisonCondition
{
	private readonly ComparisonConditionCore _core =
		new(leftSide, Operators.LessThan, rightSide);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
