namespace SqlArtisan.Internal;

public interface ISelectBuilderGroupBy : ISqlBuilder, ISetOperator, ISubquery
{
    ISelectBuilderHaving Having(SqlCondition condition);

    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);
}
