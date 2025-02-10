namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static OrCondition OR(params ICondition[] conditions) =>
		new(conditions);
}
