namespace InlineSqlSharp.Oracle;

public static partial class SqlWordbook
{
	public static NotCondition NOT(ICondition condition) => new(condition);
}
