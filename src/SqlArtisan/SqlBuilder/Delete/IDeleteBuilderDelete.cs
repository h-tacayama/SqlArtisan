namespace SqlArtisan;

public interface IDeleteBuilderDelete : ISqlBuilder
{
    IDeleteBuilderWhere Where(SqlCondition condition);
}
