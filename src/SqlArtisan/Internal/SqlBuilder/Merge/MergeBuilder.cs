namespace SqlArtisan.Internal;

internal sealed class MergeBuilder(DbTableBase target, params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IMergeBuilderOn,
    IMergeBuilderTarget,
    IMergeBuilderThenInsert,
    IMergeBuilderThenUpdateSet,
    IMergeBuilderUsing,
    IMergeBuilderWhen,
    IMergeBuilderWhenMatched,
    IMergeBuilderWhenNotMatched,
    IMergeBuilderWhenNotMatchedBySource
{
    // The column count of the most recent ThenInsert, cross-checked by the next
    // Values call — the same #397 width guard plain INSERT threads through its
    // constructor; MERGE's fluent pairing makes a field the equivalent carrier.
    private int _pendingInsertColumnCount;

    private protected override DbTableBase? CorrelatedDmlGuardTarget =>
        target.HasAlias ? null : target;

    protected override string StatementName => Keywords.Merge;

    public SqlStatement Build() => BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) => BuildCore(dbms);

    public IMergeBuilderWhen DeleteWhere(SqlCondition condition)
    {
        AddPart(new MergeDeleteWhereClause(condition));
        return this;
    }

    public IMergeBuilderOn On(SqlCondition condition)
    {
        AddPart(new MergeOnClause(condition));
        return this;
    }

    // Shared by IMergeBuilderWhenMatched and IMergeBuilderWhenNotMatchedBySource
    // (same signature and return type), so one implementation satisfies both.
    public IMergeBuilderWhen ThenDelete()
    {
        AddPart(new MergeDeleteClause());
        return this;
    }

    public IMergeBuilderThenInsert ThenInsert()
    {
        _pendingInsertColumnCount = 0;
        AddPart(new MergeInsertClause([]));
        return this;
    }

    public IMergeBuilderThenInsert ThenInsert(params DbColumn[] columns)
    {
        CollectionGuard.ThrowIfEmpty(
            columns, nameof(columns), "An INSERT column list requires at least one column.");
        CollectionGuard.ThrowIfNullElement(
            columns, nameof(columns), "An INSERT column list must not contain a null column.");
        ColumnListGuard.ThrowIfDuplicate(
            columns, "An INSERT column list must not name a column twice.");

        _pendingInsertColumnCount = columns.Length;
        AddPart(new MergeInsertClause(columns));
        return this;
    }

    // ThenUpdateSet differs only by return type between the two branch interfaces,
    // so each is implemented explicitly.
    IMergeBuilderThenUpdateSet IMergeBuilderWhenMatched.ThenUpdateSet(
        params EqualityCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments));
        return this;
    }

    IMergeBuilderWhen IMergeBuilderWhenNotMatchedBySource.ThenUpdateSet(
        params EqualityCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments));
        return this;
    }

    public IMergeBuilderUsing Using(TableReference source)
    {
        AddPart(new MergeUsingClause(source));
        return this;
    }

    public IMergeBuilderWhen Values(params object[] values)
    {
        // Checked here, not left to the resolver: the width guard below would
        // otherwise dereference a null array before any named guard runs.
        ArgumentNullException.ThrowIfNull(values);

        if (_pendingInsertColumnCount > 0
            && values.Length > 0
            && values.Length != _pendingInsertColumnCount)
        {
            throw new ArgumentException(
                $"The INSERT column list declares {_pendingInsertColumnCount} column(s), " +
                $"but this VALUES row has {values.Length} value(s).");
        }

        AddPart(InsertValuesClause.Parse(values));
        return this;
    }

    public IMergeBuilderWhenMatched WhenMatched()
    {
        AddPart(new WhenMatchedClause(null));
        return this;
    }

    public IMergeBuilderWhenMatched WhenMatched(SqlCondition extraCondition)
    {
        // A null here would silently render the unconditioned branch the
        // zero-argument overload spells on purpose.
        AddPart(new WhenMatchedClause(
            NullGuard.ThrowIfNull(extraCondition, nameof(extraCondition))));
        return this;
    }

    public IMergeBuilderWhenNotMatched WhenNotMatched()
    {
        AddPart(new WhenNotMatchedClause(null));
        return this;
    }

    public IMergeBuilderWhenNotMatched WhenNotMatched(SqlCondition extraCondition)
    {
        AddPart(new WhenNotMatchedClause(
            NullGuard.ThrowIfNull(extraCondition, nameof(extraCondition))));
        return this;
    }

    public IMergeBuilderWhenNotMatchedBySource WhenNotMatchedBySource()
    {
        AddPart(new WhenNotMatchedBySourceClause(null));
        return this;
    }

    public IMergeBuilderWhenNotMatchedBySource WhenNotMatchedBySource(SqlCondition extraCondition)
    {
        AddPart(new WhenNotMatchedBySourceClause(
            NullGuard.ThrowIfNull(extraCondition, nameof(extraCondition))));
        return this;
    }

    // SQL Server requires a MERGE to end in a semicolon; the dialect supplies it
    // (empty for every other DBMS, leaving their output unchanged).
    protected override void AppendTrailing(SqlBuildingBuffer buffer) =>
        buffer.AppendMergeTerminator();

    // Branch pairing a per-kind duplicate table cannot express: a held stage can
    // leave a WHEN with no action (`... THEN` trailing) or an INSERT with no
    // VALUES — both invalid on every dialect that has MERGE.
    protected override void Validate(Dbms dbms)
    {
        bool branchOpen = false;
        bool insertOpen = false;
        string? openClause = null;
        List<(string Clause, string Action)> branches = [];

        foreach (SqlPart part in PartsSpan)
        {
            if (part is WhenMatchedClause or WhenNotMatchedClause or WhenNotMatchedBySourceClause)
            {
                ThrowIfBranchUnfinished(branchOpen, insertOpen);
                branchOpen = true;
                openClause = ClauseSpelling(part);
            }
            else if (part is MergeUpdateSetClause)
            {
                branchOpen = false;
                branches.Add((openClause!, $"{Keywords.Update} {Keywords.Set}"));
            }
            else if (part is MergeDeleteClause)
            {
                branchOpen = false;
                branches.Add((openClause!, Keywords.Delete));
            }
            else if (part is MergeInsertClause)
            {
                branchOpen = false;
                insertOpen = true;
                branches.Add((openClause!, Keywords.Insert));
            }
            else if (part is InsertValuesClause)
            {
                insertOpen = false;
            }
        }

        ThrowIfBranchUnfinished(branchOpen, insertOpen);
        ThrowIfBranchRepeated(dbms, branches);
    }

    // Oracle takes one branch per WHEN clause whatever its action (ORA-00905);
    // SQL Server one per clause-and-action pair, so its matched UPDATE and
    // DELETE coexist; PostgreSQL stacks freely — all live-verified (#523).
    private static void ThrowIfBranchRepeated(
        Dbms dbms, List<(string Clause, string Action)> branches)
    {
        if (dbms != Dbms.Oracle && dbms != Dbms.SqlServer)
        {
            return;
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach ((string clause, string action) in branches)
        {
            if (dbms == Dbms.Oracle && !seen.Add(clause))
            {
                throw new ArgumentException(
                    $"Oracle accepts at most one {clause} branch in a MERGE; combine the branch "
                        + "conditions, or spell a matched delete as "
                        + "ThenUpdateSet(...).DeleteWhere(...).");
            }

            if (dbms == Dbms.SqlServer && !seen.Add($"{clause} {action}"))
            {
                throw new ArgumentException(
                    $"SQL Server accepts at most one {clause} branch with a {action} action in a "
                        + "MERGE; give the branches different actions, or combine their "
                        + "conditions.");
            }
        }
    }

    private static string ClauseSpelling(SqlPart part) => part switch
    {
        WhenMatchedClause => $"{Keywords.When} {Keywords.Matched}",
        WhenNotMatchedClause => $"{Keywords.When} {Keywords.Not} {Keywords.Matched}",
        _ => $"{Keywords.When} {Keywords.Not} {Keywords.Matched} {Keywords.By} {Keywords.Source}",
    };

    private static void ThrowIfBranchUnfinished(bool branchOpen, bool insertOpen)
    {
        if (branchOpen)
        {
            throw new ArgumentException(
                "A MERGE WHEN branch requires an action (UPDATE SET, DELETE, or INSERT).");
        }

        if (insertOpen)
        {
            throw new ArgumentException(
                "A MERGE INSERT action requires a VALUES row.");
        }
    }
}
