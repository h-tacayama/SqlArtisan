namespace InlineSqlSharp;

public interface ISelectBuildertWhere : ISqlBuilder, ISetOperator, ISubquery
{
    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    ISelectBuilderOrderBy OrderBy(params object[] orderByItems);
}
