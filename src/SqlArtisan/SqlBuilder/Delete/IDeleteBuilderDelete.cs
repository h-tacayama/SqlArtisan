namespace SqlArtisan;

public interface IDeleteBuilderDelete : ISqlBuilder
{
    IDeleteBuilderWhere Where(AbstractCondition condition);
}
