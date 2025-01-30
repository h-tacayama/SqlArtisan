namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderFrom : ISqlBuilder
{
	ISelectBuildertWhere WHERE(ICondition condition);
}
