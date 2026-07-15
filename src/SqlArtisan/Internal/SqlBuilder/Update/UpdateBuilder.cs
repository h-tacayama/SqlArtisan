namespace SqlArtisan.Internal;

internal sealed class UpdateBuilder(DbTableBase table, DmlJoinState state, params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IUpdateBuilderFrom,
    IUpdateBuilderFromJoinOn,
    IUpdateBuilderJoinOn,
    IUpdateBuilderJoined,
    IUpdateBuilderJoinedSet,
    IUpdateBuilderOutputInto,
    IUpdateBuilderSet,
    IUpdateBuilderSetOutput,
    IUpdateBuilderUpdate,
    IUpdateBuilderWhere
{
    private protected override DbTableBase? CorrelatedDmlGuardTarget =>
        table.HasAlias ? null : table;

    protected override string StatementName => Keywords.Update;

    public SqlStatement Build() =>
        BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) =>
        BuildCore(dbms);

    public IUpdateBuilderFrom From(params TableReference[] tables)
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

    public IUpdateBuilderJoinOn FullJoin(TableReference joined)
    {
        AddJoin(new FullJoinClause(joined));
        return this;
    }

    IUpdateBuilderFromJoinOn IUpdateBuilderFrom.FullJoin(TableReference joined)
    {
        AddJoin(new FullJoinClause(joined));
        return this;
    }

    public IUpdateBuilderJoinOn InnerJoin(TableReference joined)
    {
        AddJoin(new InnerJoinClause(joined));
        return this;
    }

    IUpdateBuilderFromJoinOn IUpdateBuilderFrom.InnerJoin(TableReference joined)
    {
        AddJoin(new InnerJoinClause(joined));
        return this;
    }

    public IUpdateBuilderSet Into(DbTableBase archive, params DbColumn[] columns)
    {
        AddPart(new OutputIntoClause(archive, columns));
        return this;
    }

    public IUpdateBuilderJoinOn LeftJoin(TableReference joined)
    {
        AddJoin(new LeftJoinClause(joined));
        return this;
    }

    IUpdateBuilderFromJoinOn IUpdateBuilderFrom.LeftJoin(TableReference joined)
    {
        AddJoin(new LeftJoinClause(joined));
        return this;
    }

    IUpdateBuilderFrom IUpdateBuilderFromJoinOn.On(SqlCondition condition)
    {
        AddPart(new OnClause(condition));
        return this;
    }

    IUpdateBuilderJoined IUpdateBuilderJoinOn.On(SqlCondition condition)
    {
        AddPart(new OnClause(condition));
        return this;
    }

    public IUpdateBuilderOutputInto Output(params object[] items)
    {
        CollectionGuard.ThrowIfEmpty(items, "OUTPUT requires at least one expression.");
        AddPart(new OutputClause(SelectItemResolver.Resolve(items)));
        return this;
    }

    public IReturningBuilder Returning(params object[] expressions) =>
        ReturningBuilder.Create(this, expressions);

    public IUpdateBuilderJoinOn RightJoin(TableReference joined)
    {
        AddJoin(new RightJoinClause(joined));
        return this;
    }

    IUpdateBuilderFromJoinOn IUpdateBuilderFrom.RightJoin(TableReference joined)
    {
        AddJoin(new RightJoinClause(joined));
        return this;
    }

    public IUpdateBuilderSetOutput Set(params EqualityBasedCondition[] assignments)
    {
        AddPart(UpdateSetClause.Parse(assignments, state));
        return this;
    }

    IUpdateBuilderJoinedSet IUpdateBuilderJoined.Set(params EqualityBasedCondition[] assignments)
    {
        AddPart(UpdateSetClause.Parse(assignments, state));
        return this;
    }

    IUpdateBuilderFrom IUpdateBuilderFromJoinOn.Using(DbColumn column, params DbColumn[] additionalColumns)
    {
        AddPart(new JoinUsingClause([column, .. additionalColumns]));
        return this;
    }

    IUpdateBuilderJoined IUpdateBuilderJoinOn.Using(DbColumn column, params DbColumn[] additionalColumns)
    {
        AddPart(new JoinUsingClause([column, .. additionalColumns]));
        return this;
    }

    public IUpdateBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    protected override void Validate(Dbms dbms)
    {
        if (state.IsJoined)
        {
            DmlTargetGuard.ThrowIfJoinedTargetUnaliased(table);
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
