namespace SqlArtisan.Internal;

public interface IDeleteBuilderDelete : ISqlBuilder
{
    IDeleteBuilderWhere Where(SqlCondition condition);
}
