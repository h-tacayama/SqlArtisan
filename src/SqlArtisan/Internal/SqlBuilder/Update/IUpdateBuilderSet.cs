namespace SqlArtisan.Internal;

public interface IUpdateBuilderSet : ISqlBuilder
{
    IUpdateBuilderWhere Where(SqlCondition condition);
}
