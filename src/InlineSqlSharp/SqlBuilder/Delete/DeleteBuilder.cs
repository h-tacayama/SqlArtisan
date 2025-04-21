namespace InlineSqlSharp;

internal sealed class DeleteBuilder(DeleteClause deleteClause) :
    AbstractSqlBuilder(deleteClause),
    IDeleteBuilderDelete,
    IDeleteBuilderWhere
{
    public SqlStatement Build() => BuildCore();

    public IDeleteBuilderWhere WHERE(AbstractCondition condition)
    {
        AddElement(new WhereClause(condition));
        return this;
    }
}
