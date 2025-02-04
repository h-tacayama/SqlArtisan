namespace InlineSqlSharp;

public sealed class InCondition(
	IExpr leftSide,
	IExpr primaryExpr,
	IExpr[] secondaryExprs) : ICondition
{
	private readonly InConditionCore _core = new(
		false,
		leftSide,
		primaryExpr,
		secondaryExprs);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
