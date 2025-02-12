namespace InlineSqlSharp;

public sealed class NotCondition(ICondition condition) : ICondition
{
	private readonly ICondition _condition = condition;

	public void FormatSql(ref SqlBuildingBuffer buffer) => buffer
		.AppendLine(Keywords.NOT)
		.EncloseInLines(_condition);
}
