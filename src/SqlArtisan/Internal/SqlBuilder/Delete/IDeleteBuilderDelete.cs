namespace SqlArtisan.Internal;

public interface IDeleteBuilderDelete : ISqlBuilder, IReturning
{
    IDeleteBuilderWhere Where(SqlCondition condition);
}
