namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderJoin : ISqlBuilder
{
	ISelectBuilderOn ON(ICondition condition);
}
