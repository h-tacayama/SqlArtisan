namespace InlineSqlSharp;

public sealed class WhereClause(ICondition condition) : ISqlElement
{
	private readonly ICondition _condition = condition;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendLine(Keywords.WHERE)
		.Append(_condition);
}