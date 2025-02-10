namespace InlineSqlSharp;

public interface ISelectBuilderGroupBy : ISqlBuilder, ISubquery
{
	ISelectBuilderOrderBy ORDER_BY(
		params IExprOrAliasOrSortOrder[] sortExpressions);
}
