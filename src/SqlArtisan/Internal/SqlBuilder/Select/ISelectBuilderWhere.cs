namespace SqlArtisan.Internal;

public interface ISelectBuilderWhere : ISqlBuilder, ISetOperator, ISubquery
{
    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    ISelectBuilderOrderBy OrderBy(params object[] orderByItems);
}
