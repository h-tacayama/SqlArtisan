namespace InlineSqlSharp;

public sealed class BetweenCondition(
	IExpr leftSide,
	IExpr rightSide1,
	IExpr rightSide2) : ICondition
{
	private readonly BetweenConditionCore _core = new(
		false,
		leftSide,
		rightSide1,
		rightSide2);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
