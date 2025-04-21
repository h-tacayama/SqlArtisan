namespace InlineSqlSharp;

public interface IDeleteBuilderDelete : ISqlBuilder
{
    IDeleteBuilderWhere WHERE(AbstractCondition condition);
}
