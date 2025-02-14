using System.Diagnostics;

namespace InlineSqlSharp;

public class SelectBuilder :
	AbstractSqlBuilder<SelectBuilder>,
	ISelectBuilderFrom,
	ISelectBuilderGroupBy,
	ISelectBuilderHaving,
	ISelectBuilderSetOperator,
	ISelectBuilderJoin,
	ISelectBuilderOrderBy,
	ISelectBuilderSelect,
	ISelectBuildertWhere
{
	public SelectBuilder(ISqlElement sqlElement) : base(sqlElement)
	{
	}

	public SelectBuilder(SelectClause selectClause) : base(selectClause)
	{
	}

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		FormatAsSubquery(ref buffer);

	public SqlCommand Build() => BuildCore();

	public ISelectBuilderFrom CROSS_JOIN(ITableReference table) =>
		AddElement(new CrossJoinClause(table));

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public ISelectBuilderSetOperator EXCEPT =>
		AddElement(new ExceptOperator(false));

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public ISelectBuilderSetOperator EXCEPT_ALL =>
		AddElement(new ExceptOperator(true));

	public ISelectBuilderFrom FROM(params ITableReference[] tables) =>
		AddElement(new FromClause(tables));

	public ISelectBuilderJoin FULL_JOIN(ITableReference table) =>
		AddElement(new FullJoinClause(table));

	public ISelectBuilderGroupBy GROUP_BY(params IExpr[] groupingExpressions) =>
		AddElement(new GroupByClause(groupingExpressions));

	public ISelectBuilderHaving HAVING(ICondition condition) =>
		AddElement(new HavingClause(condition));

	public ISelectBuilderJoin INNER_JOIN(ITableReference table) =>
		AddElement(new InnerJoinClause(table));

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public ISelectBuilderSetOperator INTERSECT =>
		AddElement(new IntersectOperator(false));

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public ISelectBuilderSetOperator INTERSECT_ALL =>
		AddElement(new IntersectOperator(true));

	public ISelectBuilderJoin LEFT_JOIN(ITableReference table) =>
		AddElement(new LeftJoinClause(table));

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public ISelectBuilderSetOperator MINUS =>
		AddElement(new MinusOperator(false));

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public ISelectBuilderSetOperator MINUS_ALL =>
		AddElement(new MinusOperator(true));
	public ISelectBuilderJoin RIGHT_JOIN(ITableReference table) =>
		AddElement(new RightJoinClause(table));

	public ISelectBuilderFrom ON(ICondition condition) =>
		AddElement(new OnClause(condition));

	public ISelectBuilderOrderBy ORDER_BY(
		params IExprOrAliasOrSortOrder[] sortExpressions) =>
		AddElement(new OrderByClause(sortExpressions));

	public ISelectBuilderSelect SELECT(params IExprOrAlias[] selectList) =>
		AddElement(new SelectClause(false, selectList));

	public ISelectBuilderSelect SELECT_DISTINCT(params IExprOrAlias[] selectList) =>
		AddElement(new SelectClause(true, selectList));

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public ISelectBuilderSetOperator UNION =>
		AddElement(new UnionOperator(false));

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public ISelectBuilderSetOperator UNION_ALL =>
		AddElement(new UnionOperator(true));

	public ISelectBuildertWhere WHERE(ICondition condition) =>
		AddElement(new WhereClause(condition));
}