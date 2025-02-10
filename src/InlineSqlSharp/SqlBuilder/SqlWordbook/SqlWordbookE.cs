namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static ExistsCondition EXISTS(ISubquery subquery) =>
		new(subquery);
}