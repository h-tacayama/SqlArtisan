namespace SqlArtisan.Internal;

public interface IUpdateBuilderSet : ISqlBuilder, IReturning
{
    IUpdateBuilderWhere Where(SqlCondition condition);
}
