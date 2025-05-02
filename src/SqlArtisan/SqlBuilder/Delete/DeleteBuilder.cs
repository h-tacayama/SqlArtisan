namespace SqlArtisan;

internal sealed class DeleteBuilder(DeleteClause deleteClause) :
    SqlBuilderBase(deleteClause),
    IDeleteBuilderDelete,
    IDeleteBuilderWhere
{
    public SqlStatement Build() => BuildCore();

    public IDeleteBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }
}
