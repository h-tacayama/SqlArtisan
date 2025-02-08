namespace InlineSqlSharp;

public sealed class WhereClause(ICondition condition) : ISqlElement
{
	private readonly ICondition _condition = condition;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.AppendLine(Keywords.WHERE)
			.FormatSql(_condition);
}