namespace SqlArtisan.Internal;

internal sealed class DeleteBuilder(DbTableBase table, DmlJoinState state, params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IDeleteBuilderDelete,
    IDeleteBuilderFrom,
    IDeleteBuilderFromJoinOn,
    IDeleteBuilderUsing,
    IDeleteBuilderWhere
{
    private protected override DbTableBase? CorrelatedDmlGuardTarget =>
        table.HasAlias ? null : table;

    protected override string StatementName => Keywords.Delete;

    public SqlStatement Build() =>
        BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) =>
        BuildCore(dbms);

    public IDeleteBuilderFrom From(params TableReference[] tables)
    {
        CollectionGuard.ThrowIfEmpty(tables, "FROM requires at least one table.");
        AddPart(new FromClause(tables));
        state.HasFrom = true;

        foreach (TableReference reference in tables)
        {
            if (ReferenceEquals(reference, table))
            {
                state.TargetRepeatedInFrom = true;
                break;
            }
        }

        return this;
    }

    public IDeleteBuilderFromJoinOn FullJoin(TableReference joined)
    {
        AddJoin(new FullJoinClause(joined));
        return this;
    }

    public IDeleteBuilderFromJoinOn InnerJoin(TableReference joined)
    {
        AddJoin(new InnerJoinClause(joined));
        return this;
    }

    public IDeleteBuilderFromJoinOn LeftJoin(TableReference joined)
    {
        AddJoin(new LeftJoinClause(joined));
        return this;
    }

    public IDeleteBuilderFrom On(SqlCondition condition)
    {
        AddPart(new OnClause(condition));
        return this;
    }

    public IReturningBuilder Returning(params object[] expressions) =>
        ReturningBuilder.Create(this, expressions);

    public IDeleteBuilderFromJoinOn RightJoin(TableReference joined)
    {
        AddJoin(new RightJoinClause(joined));
        return this;
    }

    public IDeleteBuilderUsing Using(params TableReference[] tables)
    {
        CollectionGuard.ThrowIfEmpty(tables, "USING requires at least one table.");
        AddPart(new DeleteUsingClause(tables));
        state.HasUsing = true;
        return this;
    }

    public IDeleteBuilderFrom Using(DbColumn column, params DbColumn[] additionalColumns)
    {
        AddPart(new JoinUsingClause([column, .. additionalColumns]));
        return this;
    }

    public IDeleteBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    protected override void Validate(Dbms dbms)
    {
        if (state.IsJoined)
        {
            DmlTargetGuard.ThrowIfJoinedTargetUnaliased(table);
            DmlTargetGuard.ThrowIfJoinedDeleteTargetNotRepeated(state);
        }
        else
        {
            DmlTargetGuard.ThrowIfAliasedOnSqlServer(table, dbms);
        }
    }

    private void AddJoin(SqlPart joinClause)
    {
        AddPart(joinClause);
        state.HasJoin = true;
    }
}
