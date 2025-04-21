namespace InlineSqlSharp;

public interface ISelectBuilderGroupBy : ISqlBuilder, ISetOperator, ISubquery
{
    ISelectBuilderHaving HAVING(AbstractCondition condition);

    ISelectBuilderOrderBy ORDER_BY(
        params object[] orderByItems);
}
