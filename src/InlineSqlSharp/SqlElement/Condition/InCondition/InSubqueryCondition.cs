namespace InlineSqlSharp;

public sealed class InSubqueryCondition(
	IExpr leftSide,
	ISubqueryBuilder subquey) : ICondition
{
	private readonly InSubqueryConditionCore _core = new(false, leftSide, subquey);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
