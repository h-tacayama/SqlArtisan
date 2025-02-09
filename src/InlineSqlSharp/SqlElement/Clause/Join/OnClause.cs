namespace InlineSqlSharp;

public sealed class OnClause(ICondition condition) : ISqlElement
{
	private readonly ICondition _condition = condition;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core.AppendLine(Keywords.ON)
			.Append(_condition);
}
