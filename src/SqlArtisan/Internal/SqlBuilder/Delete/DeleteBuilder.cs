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
        CollectionGuard.ThrowIfEmpty(tables, nameof(tables), "FROM requires at least one table.");
        FromClause from = new(tables);
        bool targetRepeated = false;
        foreach (TableReference reference in tables)
        {
            targetRepeated |= ReferenceEquals(reference, table);
        }

        DmlTargetGuard.ThrowIfJoinedDeleteTargetNotRepeated(targetRepeated);
        DmlTargetGuard.ThrowIfJoinedTargetUnaliased(table);

        AddPart(from);
        state.HasFrom = true;
        state.TargetRepeatedInFrom = targetRepeated;
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

    public IDeleteBuilderDelete Into(DbTableBase table, params DbColumn[] columns)
    {
        OutputIntoClause into = new(table, columns);
        OutputClauseGuard.ThrowIfIntoWidthMismatch(FindPart<OutputClause>(), into);
        AddPart(into);
        return this;
    }

    public IDeleteBuilderFromJoinOn LeftJoin(TableReference joined)
    {
        AddJoin(new LeftJoinClause(joined));
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
            items, nameof(items), "OUTPUT requires at least one expression.");
        AddPart(new OutputClause(SelectItemResolver.Resolve(items, "OutputItem", "outputItem")));
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
        CollectionGuard.ThrowIfEmpty(tables, nameof(tables), "USING requires at least one table.");
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

    // The joined form's WHERE returns a stage without RETURNING, which no engine
    // that spells DELETE ... FROM has.
    IDeleteBuilderFromWhere IDeleteBuilderFrom.Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    protected override void Validate(Dbms dbms)
    {
        DmlTargetGuard.ThrowIfLeadingWithUnsupported(PartsSpan, dbms, insert: false);
        if (!state.IsJoined)
        {
            DmlTargetGuard.ThrowIfAliasedOnSqlServer(table, dbms);
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
            DmlTargetGuard.ThrowIfSqlServerJoinedTargetNotRepeated(state, dbms, Keywords.Delete);
        }
    }

    private void AddJoin(SqlPart joinClause)
    {
        DmlTargetGuard.ThrowIfJoinedTargetUnaliased(table);
        AddPart(joinClause);
        state.HasJoin = true;
    }
}
