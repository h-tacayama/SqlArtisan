namespace SqlArtisan.Internal;

internal sealed class UpdateBuilder(DbTableBase table, params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IUpdateBuilderSet,
    IUpdateBuilderUpdate,
    IUpdateBuilderWhere
{
    protected override string StatementName => Keywords.Update;

    public SqlStatement Build() =>
        BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) =>
        BuildCore(dbms);

    public IReturningBuilder Returning(params object[] expressions) =>
        ReturningBuilder.Create(this, expressions);

    public IUpdateBuilderSet Set(params EqualityBasedCondition[] assignments)
    {
        AddPart(UpdateSetClause.Parse(assignments));
        return this;
    }

    public IUpdateBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    protected override void Validate(Dbms dbms) =>
        DmlTargetGuard.ThrowIfAliasedOnSqlServer(table, dbms);
}
