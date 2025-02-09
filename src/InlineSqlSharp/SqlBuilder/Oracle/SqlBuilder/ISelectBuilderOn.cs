namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderOn : ISqlBuilder, ISubquery
{
	ISelectBuildertWhere WHERE(ICondition condition);
}
