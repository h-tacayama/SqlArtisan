namespace InlineSqlSharp;

public sealed class NotCondition(ICondition condition) : ICondition
{
	private readonly ICondition _condition = condition;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core
			.AppendLine(Keywords.NOT)
			.EncloseInLines(_condition);
}
