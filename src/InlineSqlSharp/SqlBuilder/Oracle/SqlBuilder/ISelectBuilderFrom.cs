namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderFrom : ISqlBuilder, ISubquery
{
	ISelectBuilderJoin INNER_JOIN(ITableReference table);

	ISelectBuildertWhere WHERE(ICondition condition);
}
