namespace SqlArtisan.Internal;

internal sealed class DeleteBuilder(DbTableBase target, params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IDeleteBuilderDelete,
    IDeleteBuilderWhere
{
    public SqlStatement Build() =>
        BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) =>
        BuildCore(dbms);

    public IReturningBuilder Returning(params object[] expressions) =>
        ReturningBuilder.Create(this, expressions);

    public IDeleteBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    protected override void Validate(Dbms dbms) =>
        DmlTargetGuard.RejectAliasedTargetOnSqlServer(target, dbms);
}
