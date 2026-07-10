namespace SqlArtisan.Internal;

internal sealed class DeleteBuilder(DbTableBase table, params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IDeleteBuilderDelete,
    IDeleteBuilderWhere
{
    private protected override DbTableBase? CorrelatedDmlGuardTarget =>
        table.HasAlias ? null : table;

    protected override string StatementName => Keywords.Delete;

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
        DmlTargetGuard.ThrowIfAliasedOnSqlServer(table, dbms);
}
