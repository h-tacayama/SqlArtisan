namespace InlineSqlSharp.Oracle;

public static partial class SqlWordbook
{
	public static NotCondition NOT(ICondition condition) => new(condition);

	public static NotExistsCondition NOT_EXISTS(ISubqueryBuilder subquery) =>
		new(subquery);
}
