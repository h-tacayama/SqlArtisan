namespace InlineSqlSharp;

public sealed class NotInCondition(
	IExpr leftSide,
	IExpr[] expressions) : ICondition
{
	private readonly InConditionCore _core = new(
		true,
		leftSide,
		expressions);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
