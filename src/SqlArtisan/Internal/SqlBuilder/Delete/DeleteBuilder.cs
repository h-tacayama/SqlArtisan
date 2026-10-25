namespace SqlArtisan.Internal;

internal sealed class DeleteBuilder(
    DbTableBase table,
    DmlJoinState state,
    params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IDeleteBuilderDelete,
    IDeleteBuilderDeleteOutput,
    IDeleteBuilderFrom,
    IDeleteBuilderFromJoinOn,
    IDeleteBuilderFromWhere,
    IDeleteBuilderOutputInto,
    IDeleteBuilderUsing,
    IDeleteBuilderWhere
{
    private protected override DbTableBase? CorrelatedDmlGuardTarget =>
        table.HasAlias ? null : table;

    private protected override DbTableBase? ReturningTarget => table;

    protected override string StatementName => Keywords.Delete;

    public SqlStatement Build() =>
        BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) =>
        BuildCore(dbms);

    public IDeleteBuilderFrom From(params TableReference[] tables)
    {
        CollectionGuard.ThrowIfEmpty(
            tables, nameof(tables), "FROM requires at least one table.",
            FromClause.NullTableMessage);
        FromClause from = new(tables);
        bool targetRepeated = false;
        foreach (TableReference reference in tables)
        {
            targetRepeated |= ReferenceEquals(reference, table);
        }

        DmlTargetGuard.ThrowIfJoinedTargetUnaliased(table);

        AddPart(from);
        state.HasFrom = true;
        state.TargetRepeatedInFrom = targetRepeated;
        return this;
    }

    public IDeleteBuilderFromJoinOn FullJoin(TableReference joined)
    {
        AddJoin(new FullJoinClause(joined), joined);
        return this;
    }

    public IDeleteBuilderFromJoinOn InnerJoin(TableReference joined)
    {
        AddJoin(new InnerJoinClause(joined), joined);
        return this;
    }

    public IDeleteBuilderDelete Into(DbTableBase table, params DbColumn[] columns)
    {
        OutputIntoClause into = new(table, columns);
        OutputClauseGuard.ThrowIfIntoWidthMismatch(FindPart<OutputClause>(), into);
        AddPart(into);
        return this;
    }

    public IDeleteBuilderFromJoinOn LeftJoin(TableReference joined)
    {
        AddJoin(new LeftJoinClause(joined), joined);
        return this;
    }

    public IDeleteBuilderFrom On(SqlCondition condition)
    {
        AddPart(new DmlOnClause(condition));
        return this;
    }

    public IDeleteBuilderOutputInto Output(params object[] items)
    {
        CollectionGuard.ThrowIfEmpty(
            items, nameof(items), "OUTPUT requires at least one expression.",
            ExpressionResolver.NullValueMessage);
        AddPart(new OutputClause(SelectItemResolver.Resolve(items, "OutputItem", "outputItem")));
        return this;
    }

    public IReturningBuilder Returning(params object[] expressions) =>
        ReturningBuilder.Create(this, expressions);

    public IDeleteBuilderFromJoinOn RightJoin(TableReference joined)
    {
        AddJoin(new RightJoinClause(joined), joined);
        return this;
    }

    public IDeleteBuilderUsing Using(params TableReference[] tables)
    {
        CollectionGuard.ThrowIfEmpty(
            tables, nameof(tables), "USING requires at least one table.",
            DeleteUsingClause.NullTableMessage);
        DmlTargetGuard.ThrowIfJoinedTargetUnaliased(table);
        AddPart(new DeleteUsingClause(tables));
        state.HasUsing = true;
        return this;
    }

    public IDeleteBuilderFrom Using(DbColumn column, params DbColumn[] additionalColumns)
    {
        CollectionGuard.ThrowIfNullElement(
            additionalColumns,
            nameof(additionalColumns),
            "A USING column list must not contain a null column.");

        AddPart(new JoinUsingClause([column, .. additionalColumns], nameof(column)));
        return this;
    }

    public IDeleteBuilderWhere Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    // The joined form's WHERE returns a stage without RETURNING: no engine with one runs
    // a re-listed target, and Using(...) offers it where the target is not re-listed.
    IDeleteBuilderFromWhere IDeleteBuilderFrom.Where(SqlCondition condition)
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
            DmlTargetGuard.ThrowIfAliasedOnSqlServer(table, dbms, Keywords.Delete);
        }

        ReturningGuard.ThrowIfCombinedWithJoinedDelete(
            state, FindPart<ReturningClause>(), FindPart<ReturningIntoClause>());

        OutputClause? output = FindPart<OutputClause>();
        OutputClauseGuard.ThrowIfCombinedWithReturning(
            output, FindPart<ReturningClause>(), FindPart<ReturningIntoClause>());
        OutputClauseGuard.ThrowIfDeleteCombinedWithUsing(output, FindPart<DeleteUsingClause>());

        // Last so the dialect-independent pairing guards above report first —
        // an OUTPUT + USING statement is broken on every dialect, not just T-SQL.
        if (state.IsJoined)
        {
            DmlTargetGuard.ThrowIfSqlServerDeleteUsing(FindPart<DeleteUsingClause>(), dbms);
            DmlTargetGuard.ThrowIfJoinedDeleteTargetNotRepeated(state, dbms);
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
