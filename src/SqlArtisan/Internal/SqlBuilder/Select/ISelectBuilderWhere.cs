namespace SqlArtisan.Internal;

public interface ISelectBuilderWhere : ISqlBuilder, ISetOperator, IForUpdate, ISubquery
{
    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    ISelectBuilderOrderBy OrderBy(params object[] orderByItems);
}
