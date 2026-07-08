namespace SqlArtisan.Internal;

internal sealed class UpdateBuilder(DbTableBase table, params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IUpdateBuilderSet,
    IUpdateBuilderUpdate,
    IUpdateBuilderWhere
{
    // Held so Validate can reject an all-empty WHERE at Build() — checked then,
    // not here, because `operator &` can still make it non-empty after this call.
    private SqlCondition? _whereCondition;

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
        _whereCondition = condition;
        AddPart(new WhereClause(condition));
        return this;
    }

    protected override void Validate(Dbms dbms)
    {
        DmlTargetGuard.RejectAliasedTargetOnSqlServer(table, dbms);

        if (_whereCondition is not null)
        {
            EmptyConditionGuard.Reject(
                _whereCondition,
                "The WHERE clause of an UPDATE or DELETE requires a condition; omit it to affect every row.");
        }
    }
}
