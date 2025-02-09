namespace InlineSqlSharp.Oracle;

public class SelectBuilder :
	AbstractSqlBuilder,
	ISelectBuilderFrom,
	ISelectBuilderJoin,
	ISelectBuilderSelect,
	ISelectBuildertWhere
{
	public SelectBuilder(ISqlElement sqlElement) : base(sqlElement)
	{
	}

	public SelectBuilder(SelectClause selectClause) : base(selectClause)
	{
	}

	public SqlCommand Build() => BuildCore();

	public ISelectBuilderFrom CROSS_JOIN(ITableReference table)
	{
		AddElement(new CrossJoinClause(table));
		return this;
	}

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		FormatAsSubquery(ref buffer);

	public ISelectBuilderFrom FROM(params ITableReference[] tables)
	{
		AddElement(new FromClause(tables));
		return this;
	}

	public ISelectBuilderJoin FULL_JOIN(ITableReference table)
	{
		AddElement(new FullJoinClause(table));
		return this;
	}

	public ISelectBuilderJoin INNER_JOIN(ITableReference table)
	{
		AddElement(new InnerJoinClause(table));
		return this;
	}

	public ISelectBuilderJoin LEFT_JOIN(ITableReference table)
	{
		AddElement(new LeftJoinClause(table));
		return this;
	}

	public ISelectBuilderJoin RIGHT_JOIN(ITableReference table)
	{
		AddElement(new RightJoinClause(table));
		return this;
	}

	public ISelectBuilderFrom ON(ICondition condition)
	{
		AddElement(new OnClause(condition));
		return this;
	}

	public ISelectBuilderSelect SELECT(params IExprOrAlias[] selectList)
	{
		AddElement(new SelectClause(selectList));
		return this;
	}

	public ISelectBuildertWhere WHERE(ICondition condition)
	{
		AddElement(new WhereClause(condition));
		return this;
	}
}
