namespace InlineSqlSharp.Oracle;

public static partial class SqlWordbook
{
	public static ExistsCondition EXISTS(ISubqueryBuilder subquery) =>
		new(subquery);
}