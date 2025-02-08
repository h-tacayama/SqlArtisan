namespace InlineSqlSharp;

public sealed class OrCondition(ICondition[] conditions) : ICondition
{
	private readonly MultiLogicalConditionCore _core = new(
		Keywords.OR,
		conditions);

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		_core.FormatSql(ref buffer);
}
