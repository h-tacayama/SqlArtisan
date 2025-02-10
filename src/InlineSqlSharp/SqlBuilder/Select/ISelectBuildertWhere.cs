namespace InlineSqlSharp;

public interface ISelectBuildertWhere : ISqlBuilder, ISubquery
{
	ISelectBuilderGroupBy GROUP_BY(params IExpr[] groupingExpressions);

	ISelectBuilderOrderBy ORDER_BY(
		params IExprOrAliasOrSortOrder[] sortExpressions);
}
