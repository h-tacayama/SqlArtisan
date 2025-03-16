namespace InlineSqlSharp;

public sealed class LessThanOrEqualCondition(
	IExpr leftSide,
	IExpr rightSide) : IComparisonCondition
{
	private readonly ComparisonConditionCore _core =
		new(leftSide, Operators.LessThanOrEqual, rightSide);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
