namespace InlineSqlSharp;

public sealed class ExistsCondition(ISubqueryBuilder subquery) : ICondition
{
	private readonly ExistsConditionCore _core = new(false, subquery);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
