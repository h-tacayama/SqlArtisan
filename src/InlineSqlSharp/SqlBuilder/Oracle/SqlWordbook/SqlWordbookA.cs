namespace InlineSqlSharp.Oracle;

public static partial class SqlWordbook
{
	public static DynamicCondition AddConditionIf(
		bool addIf,
		ICondition condition) => new(addIf, condition);

	public static AndCondition AND(params ICondition[] conditions) =>
		new(conditions);
}
