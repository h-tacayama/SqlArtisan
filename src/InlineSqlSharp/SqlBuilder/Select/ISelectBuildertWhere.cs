namespace InlineSqlSharp;

public interface ISelectBuildertWhere : ISqlBuilder, ISubquery
{
	ISelectBuilderOrderBy ORDER_BY(
		params IExprOrAliasOrSortOrder[] sortExpressions);
}
