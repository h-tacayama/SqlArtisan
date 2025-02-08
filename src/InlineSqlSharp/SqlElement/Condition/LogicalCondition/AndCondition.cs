namespace InlineSqlSharp;

public sealed class AndCondition(ICondition[] conditions) : ICondition
{
	private readonly MultiLogicalConditionCore _core = new(
		Keywords.AND,
		conditions);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
