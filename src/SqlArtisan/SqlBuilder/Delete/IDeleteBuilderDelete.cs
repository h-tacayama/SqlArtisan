namespace InlineSqlSharp;

public interface IDeleteBuilderDelete : ISqlBuilder
{
    IDeleteBuilderWhere Where(AbstractCondition condition);
}
