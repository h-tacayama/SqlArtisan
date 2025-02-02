namespace InlineSqlSharp;

public abstract class MultiLogicalCondition(
	string @operator,
	ICondition[] conditions) : ICondition
{
	private readonly string _operator = @operator;
	private readonly ICondition[] _conditions = conditions;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		bool added = false;

		for (int i = 0; i < _conditions.Length; i++)
		{
			if (_conditions[i] is DynamicCondition dc && !dc.AddIf)
			{
				continue;
			}

			if (added)
			{
				buffer.AppendLine();
				buffer.AppendLine(_operator);
			}

			buffer.AppendLine("(");
			_conditions[i].FormatSql(ref buffer);
			buffer.AppendLine();
			buffer.Append(")");

			added = true;
		}
	}
}
