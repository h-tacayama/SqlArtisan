namespace InlineSqlSharp;

public sealed class InCondition(
	IExpr leftSide,
	IExpr[] expressions) : ICondition
{
	private readonly InConditionCore _core = new(
		false,
		leftSide,
		expressions);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
