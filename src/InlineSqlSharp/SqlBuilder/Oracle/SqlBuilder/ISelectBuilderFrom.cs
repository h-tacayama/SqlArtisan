namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderFrom : ISqlBuilder, ISubqueryBuilder
{
	ISelectBuildertWhere WHERE(ICondition condition);
}
