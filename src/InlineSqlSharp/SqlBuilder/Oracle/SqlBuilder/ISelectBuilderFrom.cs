namespace InlineSqlSharp.Oracle;

public interface ISelectBuilderFrom : ISqlBuilder, ISubquery
{
	// Subsequent SQL is the same as the FROM clause.
	ISelectBuilderFrom CROSS_JOIN(ITableReference table);

	ISelectBuilderJoin FULL_JOIN(ITableReference table);

	ISelectBuilderJoin INNER_JOIN(ITableReference table);

	ISelectBuilderJoin LEFT_JOIN(ITableReference table);

	ISelectBuilderJoin RIGHT_JOIN(ITableReference table);

	ISelectBuildertWhere WHERE(ICondition condition);
}
