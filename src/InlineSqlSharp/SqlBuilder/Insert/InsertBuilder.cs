namespace InlineSqlSharp;
internal sealed class InsertBuilder
	: SelectBuilder,
	IInsertBuilderInsertInto,
	IInsertBuilderSelect,
	IInsertBuilderSet
{
	internal InsertBuilder(InsertIntoClause insertIntoClause)
		: base(insertIntoClause)
	{
	}

	internal InsertBuilder(InsertSelectClause insertSelectClause)
		: base(insertSelectClause)
	{
	}

	public IInsertBuilderSet SET(params IEquality[] assignments)
	{
		AddElement(new InsertSetClause(assignments));
		return this;
	}
}
