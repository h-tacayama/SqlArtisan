namespace InlineSqlSharp.Oracle;

public static partial class SqlWordbook
{
	public static AndCondition AND(params ICondition[] conditions) =>
		new(conditions);
}
