namespace SqlArtisan.Internal;

internal class DeleteBuilder(params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IDeleteBuilderDelete,
    IDeleteBuilderWhere
{
    public SqlStatement Build() =>
        BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) =>
        BuildCore(dbms);

    public IDeleteBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }
}
