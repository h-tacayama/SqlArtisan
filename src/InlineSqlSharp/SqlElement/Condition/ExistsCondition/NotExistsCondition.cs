namespace InlineSqlSharp;

public sealed class NotExistsCondition(ISubquery subquery) : ICondition
{
	private readonly ExistsConditionCore _core = new(true, subquery);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
