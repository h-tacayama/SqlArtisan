namespace InlineSqlSharp.Oracle;

public class SelectBuilder :
	AbstractSqlBuilder,
	ISelectBuilderFrom,
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

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		FormatAsSubquery(ref buffer);

	public ISelectBuilderFrom FROM(params ITableReference[] tables)
	{
		AddElement(new FromClause(tables));
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
