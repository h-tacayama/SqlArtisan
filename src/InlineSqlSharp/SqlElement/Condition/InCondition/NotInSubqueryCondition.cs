namespace InlineSqlSharp;

public sealed class NotInSubqueryCondition(
	IExpr leftSide,
	ISubqueryBuilder subquey) : ICondition
{
	private readonly InSubqueryConditionCore _core = new(true, leftSide, subquey);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
