namespace InlineSqlSharp;

public sealed class OrCondition(ICondition[] conditions) : ICondition
{
	private readonly MultiLogicalConditionCore _core = new(
		Keywords.OR,
		conditions);

	public void FormatSql(SqlBuildingBuffer buffer) =>
		_core.FormatSql(buffer);
}
