namespace InlineSqlSharp;

internal sealed class HavingClause(ICondition condition) : ISqlElement
{
	private readonly ICondition _condition = condition;

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.HAVING)
		.Append(_condition);
}
