namespace InlineSqlSharp;

internal sealed class DeleteBuilder(DeleteClause deleteClause) :
    AbstractSqlBuilder(deleteClause),
    IDeleteBuilderDelete,
    IDeleteBuilderWhere
{
    public SqlStatement Build() => BuildCore();

    public IDeleteBuilderWhere WHERE(AbstractCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }
}
