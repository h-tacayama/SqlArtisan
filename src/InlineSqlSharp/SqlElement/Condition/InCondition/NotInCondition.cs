namespace InlineSqlSharp;

public sealed class NotInCondition(
	IExpr leftSide,
	IExpr primaryExpr,
	IExpr[] secondaryExprs) : ICondition
{
	private readonly InConditionCore _core = new(
		true,
		leftSide,
		primaryExpr,
		secondaryExprs);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
