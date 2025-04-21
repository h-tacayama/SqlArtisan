namespace InlineSqlSharp;

public interface IUpdateBuilderSet : ISqlBuilder
{
    IUpdateBuilderWhere WHERE(AbstractCondition condition);
}
