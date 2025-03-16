namespace InlineSqlSharp;

public interface IUpdateBuilderSet : ISqlBuilder
{
	IUpdateBuilderWhere WHERE(ICondition condition);
}
