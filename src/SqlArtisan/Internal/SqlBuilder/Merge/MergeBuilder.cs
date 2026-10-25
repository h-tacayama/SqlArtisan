namespace SqlArtisan.Internal;

internal sealed class MergeBuilder(DbTableBase target, params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IMergeBuilderOn,
    IMergeBuilderTarget,
    IMergeBuilderThenInsert,
    IMergeBuilderThenUpdateSet,
    IMergeBuilderUsing,
    IMergeBuilderValues,
    IMergeBuilderWhen,
    IMergeBuilderWhenMatched,
    IMergeBuilderWhenMatchedAnd,
    IMergeBuilderWhenNotMatched,
    IMergeBuilderWhenNotMatchedAnd,
    IMergeBuilderWhenNotMatchedAndThenInsert,
    IMergeBuilderWhenNotMatchedBySource,
    IMergeBuilderWhere
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
        CollectionGuard.ThrowIfNullElement(
            columns, nameof(columns), "An INSERT column list must not contain a null column.");
        CollectionGuard.ThrowIfEmpty(
            columns, nameof(columns), "An INSERT column list requires at least one column.");
        ColumnListGuard.ThrowIfDuplicate(
            columns, "An INSERT column list must not name a column twice.");
        ColumnListGuard.ThrowIfForeignColumn(
            columns, target, "An INSERT column list must name only columns of the target table.");

        _pendingInsertColumnCount = columns.Length;
        AddPart(new MergeInsertClause(columns));
        return this;
    }

    // These differ only by return type between branch interfaces; a conditioned or
    // BY SOURCE branch returns the branch end, without Oracle's action filters.
    IMergeBuilderWhenNotMatchedAndThenInsert IMergeBuilderWhenNotMatchedAnd.ThenInsert()
    {
        ThenInsert();
        return this;
    }

    IMergeBuilderWhenNotMatchedAndThenInsert IMergeBuilderWhenNotMatchedAnd.ThenInsert(
        params DbColumn[] columns)
    {
        ThenInsert(columns);
        return this;
    }

    IMergeBuilderThenUpdateSet IMergeBuilderWhenMatched.ThenUpdateSet(
        params EqualityCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments, target));
        return this;
    }

    IMergeBuilderWhen IMergeBuilderWhenMatchedAnd.ThenUpdateSet(
        params EqualityCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments, target));
        return this;
    }

    IMergeBuilderWhen IMergeBuilderWhenNotMatchedBySource.ThenUpdateSet(
        params EqualityCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments, target));
        return this;
    }

    public IMergeBuilderUsing Using(TableReference source)
    {
        AddPart(new MergeUsingClause(source));
        return this;
    }

    public IMergeBuilderValues Values(params object[] values)
    {
        // Checked here, not left to the resolver: the width guard below would
        // otherwise dereference a null array before any named guard runs.
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values), InsertValueResolver.NullRowMessage);
        }

        InsertValueResolver.ThrowIfArrayValue(values, severalRows: null);

        if (_pendingInsertColumnCount > 0
            && values.Length > 0
            && values.Length != _pendingInsertColumnCount)
        {
            throw new ArgumentException(
                $"The INSERT column list declares {_pendingInsertColumnCount} column(s), "
                + $"but this VALUES row has {values.Length} value(s).");
        }

        AddPart(InsertValuesClause.Parse(values));
        return this;
    }

    IMergeBuilderWhen IMergeBuilderWhenNotMatchedAndThenInsert.Values(params object[] values)
    {
        Values(values);
        return this;
    }

    public IMergeBuilderWhenMatched WhenMatched()
    {
        AddPart(new WhenMatchedClause(null));
        return this;
    }

    public IMergeBuilderWhenMatchedAnd WhenMatched(SqlCondition extraCondition)
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

    public IMergeBuilderWhenNotMatchedAnd WhenNotMatched(SqlCondition extraCondition)
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

    // Both stages declare Where(SqlCondition) with different return types, so each is
    // explicit; the two append different clauses.
    IMergeBuilderWhere IMergeBuilderThenUpdateSet.Where(SqlCondition condition)
    {
        AddPart(new MergeUpdateWhereClause(condition));
        return this;
    }

    IMergeBuilderWhen IMergeBuilderValues.Where(SqlCondition condition)
    {
        AddPart(new MergeInsertWhereClause(condition));
        return this;
    }

    protected override void AppendTrailing(SqlBuildingBuffer buffer) =>
        buffer.AppendMergeTerminator();

    // Branch pairing a per-kind duplicate table cannot express: a held stage can leave
    // a WHEN with no action or an INSERT with no VALUES, invalid wherever MERGE runs, or
    // land Oracle's action filter in the conditioned or BY SOURCE branch its type withholds.
    protected override void Validate(Dbms dbms)
    {
        DmlTargetGuard.ThrowIfLeadingWithBeforeMergeActionFilter(PartsSpan);
        DmlTargetGuard.ThrowIfLeadingWithUnsupportedOnMerge(PartsSpan, dbms);

        SqlPart? openBranch = null;
        bool insertOpen = false;
        bool filterable = false;
        bool filtered = false;
        bool refusedByOracle = false;

        foreach (SqlPart part in PartsSpan)
        {
            if (part is WhenMatchedClause or WhenNotMatchedClause or WhenNotMatchedBySourceClause)
            {
                ThrowIfBranchUnfinished(openBranch, insertOpen);
                openBranch = part;
                filterable = part is WhenMatchedClause { IsConditioned: false }
                    or WhenNotMatchedClause { IsConditioned: false };
                refusedByOracle |= !filterable;
            }
            else if (part is MergeUpdateSetClause or MergeDeleteClause)
            {
                openBranch = null;
                refusedByOracle |= part is MergeDeleteClause;
            }
            else if (part is MergeInsertClause)
            {
                openBranch = null;
                insertOpen = true;
            }
            else if (part is InsertValuesClause)
            {
                insertOpen = false;
            }
            else if (!filterable
                && part
                    is MergeUpdateWhereClause or MergeDeleteWhereClause or MergeInsertWhereClause)
            {
                throw new ArgumentException(
                    "Oracle's MERGE action WHERE and DELETE WHERE belong to a WHEN MATCHED or "
                        + "WHEN NOT MATCHED branch without AND; a stage on a held builder "
                        + "attached one to another branch.");
            }

            filtered |= part
                is MergeUpdateWhereClause or MergeDeleteWhereClause or MergeInsertWhereClause;
        }

        ThrowIfBranchUnfinished(openBranch, insertOpen);

        // The filters are Oracle's, which refuses the other branch forms, and the engines
        // with those refuse an action WHERE, so no target runs both in one MERGE (#679).
        if (filtered && refusedByOracle)
        {
            throw new ArgumentException(
                "Oracle's MERGE action WHERE and DELETE WHERE cannot share a MERGE with a "
                    + "WHEN ... AND, THEN DELETE, or BY SOURCE branch; filter every branch with "
                    + "Where(...) on Oracle, or with WhenMatched(...) / WhenNotMatched(...) "
                    + "elsewhere.");
        }
    }

    // Each WHEN clause takes its own actions, so the message lists that branch's.
    private static void ThrowIfBranchUnfinished(SqlPart? openBranch, bool insertOpen)
    {
        if (openBranch is not null)
        {
            throw new ArgumentException(openBranch switch
            {
                WhenNotMatchedClause =>
                    "A MERGE WHEN NOT MATCHED branch requires an action (INSERT).",
                WhenNotMatchedBySourceClause => "A MERGE WHEN NOT MATCHED BY SOURCE branch "
                    + "requires an action (UPDATE SET or DELETE).",
                _ => "A MERGE WHEN MATCHED branch requires an action (UPDATE SET or DELETE).",
            });
        }

        if (insertOpen)
        {
            throw new ArgumentException("A MERGE INSERT action requires a VALUES row.");
        }
    }
}
