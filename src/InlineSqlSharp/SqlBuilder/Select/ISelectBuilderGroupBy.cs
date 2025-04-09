namespace InlineSqlSharp;

public interface ISelectBuilderGroupBy : ISqlBuilder, ISubquery, ISetOperator
{
    ISelectBuilderHaving HAVING(ICondition condition);

    ISelectBuilderOrderBy ORDER_BY(
        params IExprOrAliasOrSortOrder[] sortExpressions);
}
