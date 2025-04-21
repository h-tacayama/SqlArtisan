namespace InlineSqlSharp;

public interface ISelectBuildertWhere : ISqlBuilder, ISetOperator, ISubquery
{
    ISelectBuilderGroupBy GROUP_BY(params object[] groupByItems);

    ISelectBuilderOrderBy ORDER_BY(params object[] orderByItems);
}
