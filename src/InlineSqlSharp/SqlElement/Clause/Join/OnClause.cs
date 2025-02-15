namespace InlineSqlSharp;

public sealed class OnClause(ICondition condition) : ISqlElement
{
	private readonly ICondition _condition = condition;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendLine(Keywords.ON)
		.Append(_condition);
}
