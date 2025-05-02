namespace SqlArtisan;

public interface IUpdateBuilderSet : ISqlBuilder
{
    IUpdateBuilderWhere Where(SqlCondition condition);
}
