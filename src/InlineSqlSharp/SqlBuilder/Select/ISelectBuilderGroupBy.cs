namespace InlineSqlSharp;

public interface ISelectBuilderGroupBy : ISqlBuilder, ISubquery
{
	ISelectBuilderHaving HAVING(ICondition condition);

	ISelectBuilderOrderBy ORDER_BY(
		params IExprOrAliasOrSortOrder[] sortExpressions);
}
