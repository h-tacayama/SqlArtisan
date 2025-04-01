namespace InlineSqlSharp;

internal sealed class UpdateBuilder(UpdateClause updateClause) :
	AbstractSqlBuilder(updateClause),
	IUpdateBuilderSet,
	IUpdateBuilderUpdate,
	IUpdateBuilderWhere
{
	public SqlStatement Build() => BuildCore();

	public IUpdateBuilderSet SET(params IEquality[] assignments)
	{
		AddElement(new UpdateSetClause(assignments));
		return this;
	}

	public IUpdateBuilderWhere WHERE(ICondition condition)
	{
		AddElement(new WhereClause(condition));
		return this;
	}
}
