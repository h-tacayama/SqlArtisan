namespace SqlArtisan.Internal;

public interface ISelectBuilderFrom : ISqlBuilder, IJoinOperator, ISetOperator, IForUpdate, ISubquery, IPagination
{
    ISelectBuilderGroupBy GroupBy(params object[] groupByItems);

    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);

    ISelectBuilderWhere Where(SqlCondition condition);
}
