namespace InlineSqlSharp;

public sealed class HavingClause(ICondition condition) : ISqlElement
{
	private readonly ICondition _condition = condition;

	public void FormatSql(ref SqlBuildingBuffer buffer) => buffer
		.AppendLine(Keywords.HAVING)
		.Append(_condition);
}
