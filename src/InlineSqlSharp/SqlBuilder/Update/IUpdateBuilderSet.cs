namespace InlineSqlSharp;

public interface IUpdateBuilderSet : ISqlBuilder
{
    IUpdateBuilderWhere Where(AbstractCondition condition);
}
