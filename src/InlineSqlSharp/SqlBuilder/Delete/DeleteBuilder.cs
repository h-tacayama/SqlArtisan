namespace InlineSqlSharp;

internal sealed class DeleteBuilder(DeleteClause deleteClause) :
	AbstractSqlBuilder(deleteClause),
	IDeleteBuilderDelete,
	IDeleteBuilderWhere
{
	public SqlCommand Build() => BuildCore();

	public IDeleteBuilderWhere WHERE(ICondition condition)
	{
		AddElement(new WhereClause(condition));
		return this;
	}
}
