namespace InlineSqlSharp;

public sealed class DynamicCondition(bool addIf, ICondition condition) : ICondition
{
	private readonly ICondition _condition = condition;

	public bool AddIf { get; } = addIf;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		if (!AddIf)
		{
			return;
		}

		_condition.FormatSql(ref buffer);
	}
}
