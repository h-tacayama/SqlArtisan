namespace SqlArtisan.Internal;

public interface ISelectBuilderHaving : ISqlBuilder, ISetOperator, ISubquery, IPagination
{
    ISelectBuilderOrderBy OrderBy(
        params object[] orderByItems);
}
