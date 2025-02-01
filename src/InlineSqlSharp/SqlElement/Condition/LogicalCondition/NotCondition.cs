namespace InlineSqlSharp;

public sealed class NotCondition(ICondition condition) : ICondition
{
	private readonly ICondition _condition = condition;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		buffer.AppendLine(Keywords.NOT);
		buffer.AppendLine("(");
		_condition.FormatSql(ref buffer);
		buffer.AppendLine();
		buffer.Append(")");
	}
}
