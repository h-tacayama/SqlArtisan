namespace InlineSqlSharp;

public static partial class SqlWordbook
{
	public static OrCondition OR(params ICondition[] conditions) =>
		new(conditions);

	public static PartitionAndOrderByClause ORDER_BY(
		params IExprOrAliasOrSortOrder[] sortExpressions) =>
		PartitionAndOrderByClause.Of(new OrderByClause(sortExpressions));
}
