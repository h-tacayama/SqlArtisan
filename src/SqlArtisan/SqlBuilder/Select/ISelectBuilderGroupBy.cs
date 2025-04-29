namespace SqlArtisan;

public interface ISelectBuilderGroupBy : ISqlBuilder, ISetOperator, ISubquery
{
    ISelectBuilderHaving Having(AbstractCondition condition);

    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);
}
