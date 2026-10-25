namespace SqlArtisan.Internal;

internal sealed class UpdateBuilder(
    DbTableBase table,
    DmlJoinState state,
    params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IUpdateBuilderFrom,
    IUpdateBuilderFromJoinOn,
    IUpdateBuilderJoinOn,
    IUpdateBuilderJoined,
    IUpdateBuilderJoinedSet,
    IUpdateBuilderJoinedWhere,
    IUpdateBuilderOutputInto,
    IUpdateBuilderSet,
    IUpdateBuilderSetOutput,
    IUpdateBuilderUpdate,
    IUpdateBuilderWhere
{
    private protected override DbTableBase? CorrelatedDmlGuardTarget =>
        table.HasAlias ? null : table;

    private protected override DbTableBase? ReturningTarget => table;

    protected override string StatementName => Keywords.Update;

    public SqlStatement Build() =>
        BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) =>
        BuildCore(dbms);

    public IUpdateBuilderFrom From(params TableReference[] tables)
    {
        CollectionGuard.ThrowIfEmpty(tables, nameof(tables), "FROM requires at least one table.");
        DmlTargetGuard.ThrowIfJoinedTargetUnaliased(table);
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

    IUpdateBuilderFromJoinOn IUpdateBuilderFrom.FullJoin(TableReference joined)
    {
        AddJoin(new FullJoinClause(joined), joined);
        return this;
    }

    public IUpdateBuilderJoinOn InnerJoin(TableReference joined)
    {
        AddJoin(new InnerJoinClause(joined), joined);
        state.HasDirectJoin = true;
        return this;
    }

    IUpdateBuilderFromJoinOn IUpdateBuilderFrom.InnerJoin(TableReference joined)
    {
        AddJoin(new InnerJoinClause(joined), joined);
        return this;
    }

    public IUpdateBuilderSet Into(DbTableBase table, params DbColumn[] columns)
    {
        OutputIntoClause into = new(table, columns);
        OutputClauseGuard.ThrowIfIntoWidthMismatch(FindPart<OutputClause>(), into);
        AddPart(into);
        return this;
    }

    public IUpdateBuilderJoinOn LeftJoin(TableReference joined)
    {
        AddJoin(new LeftJoinClause(joined), joined);
        state.HasDirectJoin = true;
        return this;
    }

    IUpdateBuilderFromJoinOn IUpdateBuilderFrom.LeftJoin(TableReference joined)
    {
        AddJoin(new LeftJoinClause(joined), joined);
        return this;
    }

    IUpdateBuilderFrom IUpdateBuilderFromJoinOn.On(SqlCondition condition)
    {
        AddPart(new DmlOnClause(condition));
        return this;
    }

    IUpdateBuilderJoined IUpdateBuilderJoinOn.On(SqlCondition condition)
    {
        AddPart(new DmlOnClause(condition));
        return this;
    }

    public IUpdateBuilderOutputInto Output(params object[] items)
    {
        CollectionGuard.ThrowIfEmpty(
            items, nameof(items), "OUTPUT requires at least one expression.");
        AddPart(new OutputClause(SelectItemResolver.Resolve(items, "OutputItem", "outputItem")));
        return this;
    }

    public IReturningBuilder Returning(params object[] expressions) =>
        ReturningBuilder.Create(this, expressions);

    public IUpdateBuilderJoinOn RightJoin(TableReference joined)
    {
        AddJoin(new RightJoinClause(joined), joined);
        state.HasDirectJoin = true;
        return this;
    }

    IUpdateBuilderFromJoinOn IUpdateBuilderFrom.RightJoin(TableReference joined)
    {
        AddJoin(new RightJoinClause(joined), joined);
        return this;
    }

    public IUpdateBuilderSetOutput Set(params EqualityCondition[] assignments)
    {
        AddPart(UpdateSetClause.Parse(assignments, table, state));
        return this;
    }

    IUpdateBuilderJoinedSet IUpdateBuilderJoined.Set(params EqualityCondition[] assignments)
    {
        AddPart(UpdateSetClause.Parse(assignments, table, state));
        return this;
    }

    IUpdateBuilderFrom IUpdateBuilderFromJoinOn.Using(
        DbColumn column,
        params DbColumn[] additionalColumns)
    {
        CollectionGuard.ThrowIfNullElement(
            additionalColumns,
            nameof(additionalColumns),
            "A USING column list must not contain a null column.");

        AddPart(new JoinUsingClause([column, .. additionalColumns], nameof(column)));
        return this;
    }

    IUpdateBuilderJoined IUpdateBuilderJoinOn.Using(
        DbColumn column,
        params DbColumn[] additionalColumns)
    {
        CollectionGuard.ThrowIfNullElement(
            additionalColumns,
            nameof(additionalColumns),
            "A USING column list must not contain a null column.");

        AddPart(new JoinUsingClause([column, .. additionalColumns], nameof(column)));
        return this;
    }

    public IUpdateBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    // The direct-join form's WHERE returns a stage without RETURNING, which MySQL,
    // the only engine with the form, does not have.
    IUpdateBuilderJoinedWhere IUpdateBuilderJoinedSet.Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    protected override void Validate(Dbms dbms)
    {
        ReturningGuard.ThrowIfIntoAfterLeadingWith(PartsSpan, FindPart<ReturningIntoClause>());
        DmlTargetGuard.ThrowIfLeadingWithUnsupported(PartsSpan, dbms, insert: false);
        if (!state.IsJoined)
        {
            DmlTargetGuard.ThrowIfAliasedOnSqlServer(table, dbms, Keywords.Update);
        }

        DmlTargetGuard.ThrowIfUpdateJoinSpellingsMixed(state);
        ReturningGuard.ThrowIfCombinedWithJoinedUpdate(
            state, FindPart<ReturningClause>(), FindPart<ReturningIntoClause>());

        OutputClause? output = FindPart<OutputClause>();
        OutputClauseGuard.ThrowIfCombinedWithReturning(
            output, FindPart<ReturningClause>(), FindPart<ReturningIntoClause>());
        OutputClauseGuard.ThrowIfUpdateCombinedWithDirectJoin(output, state);
        OutputClauseGuard.ThrowIfUpdateCombinedWithUnrelistedFrom(output, state);

        // Last so the dialect-independent pairing guard above reports first.
        if (state.IsJoined)
        {
            DmlTargetGuard.ThrowIfSqlServerJoinedTargetNotRepeated(state, dbms, Keywords.Update);
            DmlTargetGuard.ThrowIfUpdateTargetRepeatedOffSqlServer(state, dbms);
        }
    }

    private void AddJoin(SqlPart joinClause, TableReference joined)
    {
        DmlTargetGuard.ThrowIfJoinedTargetUnaliased(table);
        AddPart(joinClause);
        state.HasJoin = true;
        // Joined after FROM, the target is re-listed as in From(target, ...) (#679).
        state.TargetRepeatedInFrom |= state.HasFrom && ReferenceEquals(joined, table);
    }
}
