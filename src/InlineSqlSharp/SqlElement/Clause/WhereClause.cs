namespace InlineSqlSharp;

internal sealed class WhereClause(ICondition condition) : ISqlElement
{
	private readonly ICondition _condition = condition;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.WHERE)
		.Append(_condition);
}