namespace SqlArtisan.Internal;

public interface ISelectBuilderWhere : ISqlBuilder, ISetOperator, IForUpdate, ISubquery, IPagination
{
    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    ISelectBuilderOrderBy OrderBy(params object[] orderByItems);
}
