namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderFrom : ISqlBuilder, ISubquery
{
	ISelectBuildertWhere WHERE(ICondition condition);
}
