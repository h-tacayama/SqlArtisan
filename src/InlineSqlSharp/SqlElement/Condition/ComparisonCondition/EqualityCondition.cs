namespace InlineSqlSharp;

public sealed class EqualityCondition(
	IExpr leftSide,
	IExpr rightSide) : IEqualityCondition
{
	private readonly ComparisonConditionCore _core =
		new(leftSide, Operators.Equality, rightSide);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
