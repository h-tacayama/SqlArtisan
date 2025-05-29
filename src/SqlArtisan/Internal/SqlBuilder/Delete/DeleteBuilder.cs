namespace SqlArtisan.Internal;

internal sealed class DeleteBuilder(DeleteClause deleteClause) :
    SqlBuilderBase(deleteClause),
    IDeleteBuilderDelete,
    IDeleteBuilderWhere
{
    public SqlStatement Build(Dbms dbms) =>
        BuildCore(dbms);

    public IDeleteBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }
}
