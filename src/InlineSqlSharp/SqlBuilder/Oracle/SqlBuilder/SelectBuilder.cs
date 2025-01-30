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

	public ISelectBuilderFrom FROM(
		ITableReference primaryTable,
		params ITableReference[] secondaryTables)
	{
		AddElement(new FromClause(primaryTable, secondaryTables));
		return this;
	}

	public ISelectBuilderSelect SELECT(
		IExprOrAlias primaryItem,
		params IExprOrAlias[] secondaryItems)
	{
		AddElement(new SelectClause(primaryItem, secondaryItems));
		return this;
	}

	public ISelectBuildertWhere WHERE(ICondition condition)
	{
		AddElement(new WhereClause(condition));
		return this;
	}
}
