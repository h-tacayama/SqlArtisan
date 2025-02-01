namespace InlineSqlSharp;

public abstract class MultiLogicalCondition(
	string @operator,
	ICondition[] conditions) : ICondition
{
	private readonly string _operator = @operator;
	private readonly ICondition[] _conditions = conditions;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		for (int i = 0; i < _conditions.Length; i++)
		{
			if (i > 0)
			{
				buffer.AppendLine();
				buffer.AppendLine(_operator);
			}

			buffer.AppendLine("(");
			_conditions[i].FormatSql(ref buffer);
			buffer.AppendLine();
			buffer.Append(")");
		}
	}
}
