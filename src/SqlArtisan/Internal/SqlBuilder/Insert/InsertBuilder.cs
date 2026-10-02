namespace SqlArtisan.Internal;

internal sealed class InsertBuilder(
    DbTableBase table,
    int columnCount,
    params SqlPart[] rootParts) :
    SelectBuilder(rootParts),
    IInsertBuilderColumns,
    IInsertBuilderColumnsOutput,
    IInsertBuilderDoUpdateSet,
    IInsertBuilderOnConflict,
    IInsertBuilderOutputInto,
    IInsertBuilderSet,
    IInsertBuilderTable,
    IInsertBuilderValues,
    IInsertIgnoreBuilderColumns,
    IInsertIgnoreBuilderSet,
    IInsertIgnoreBuilderTable,
    IInsertIgnoreBuilderValues
{
    private const string NoRowsMessage =
        "VALUES requires at least one row; the row collection is empty.";

    private InsertValuesClause? _valuesClause;

    protected override string StatementName => Keywords.Insert;

    public IReturning DoNothing()
    {
        AddPart(new DoNothingClause());
        return this;
    }

    public IInsertBuilderDoUpdateSet DoUpdateSet(params EqualityCondition[] assignments)
    {
        AddPart(DoUpdateSetClause.Parse(assignments));
        return this;
    }

    public IInsertBuilderColumns Into(DbTableBase table, params DbColumn[] columns)
    {
        OutputIntoClause into = new(table, columns);
        OutputClauseGuard.ThrowIfIntoWidthMismatch(FindPart<OutputClause>(), into);
        AddPart(into);
        return this;
    }

    public IInsertBuilderOnConflict OnConflict(params DbColumn[] conflictTarget)
    {
        AddPart(new OnConflictClause(conflictTarget));
        return this;
    }

    public ISqlBuilder OnDuplicateKeyUpdate(params EqualityCondition[] assignments)
    {
        // Parse first: a throw after AddPart(RowAliasClause) would leave the alias, and a
        // fix-up retry on the same instance would emit it twice (`AS new AS new`).
        OnDuplicateKeyUpdateClause parsed = OnDuplicateKeyUpdateClause.Parse(assignments);
        AddPart(new RowAliasClause());
        AddPart(parsed);
        return this;
    }

    public IInsertBuilderOutputInto Output(params object[] items)
    {
        CollectionGuard.ThrowIfEmpty(
            items, nameof(items), "OUTPUT requires at least one expression.");
        AddPart(new OutputClause(SelectItemResolver.Resolve(items, "OutputItem", "outputItem")));
        return this;
    }

    public IReturningBuilder Returning(params object[] expressions) =>
        ReturningBuilder.Create(this, expressions);

    public IInsertBuilderSet Set(params EqualityCondition[] assignments)
    {
        AddPart(InsertSetClause.Parse(assignments));
        return this;
    }

    // The INSERT IGNORE stages drop IReturning (MySQL has none) and IUpsert (an
    // upsert after IGNORE is not offered; public-api-design.md § "Opinions…").
    IInsertIgnoreBuilderSet IInsertIgnoreBuilderTable.Set(params EqualityCondition[] assignments) =>
        (IInsertIgnoreBuilderSet)Set(assignments);

    public IInsertBuilderValues Values(params object[] values)
    {
        ThrowIfBuilt();
        AddValuesRow(values);
        return this;
    }

    public IInsertBuilderValues Values(IEnumerable<object[]> rows)
    {
        ThrowIfBuilt();
        ArgumentNullException.ThrowIfNull(rows);
        AddValuesRows(rows);
        return this;
    }

    public IInsertBuilderValues Values(object[][] rows)
    {
        ThrowIfBuilt();
        ArgumentNullException.ThrowIfNull(rows);
        AddValuesRows(rows);
        return this;
    }

    IInsertIgnoreBuilderValues IInsertIgnoreBuilderColumns.Values(params object[] values) =>
        (IInsertIgnoreBuilderValues)Values(values);

    IInsertIgnoreBuilderValues IInsertIgnoreBuilderColumns.Values(IEnumerable<object[]> rows) =>
        (IInsertIgnoreBuilderValues)Values(rows);

    IInsertIgnoreBuilderValues IInsertIgnoreBuilderColumns.Values(object[][] rows) =>
        (IInsertIgnoreBuilderValues)Values(rows);

    IInsertIgnoreBuilderValues IInsertIgnoreBuilderTable.Values(params object[] values) =>
        (IInsertIgnoreBuilderValues)Values(values);

    IInsertIgnoreBuilderValues IInsertIgnoreBuilderTable.Values(IEnumerable<object[]> rows) =>
        (IInsertIgnoreBuilderValues)Values(rows);

    IInsertIgnoreBuilderValues IInsertIgnoreBuilderTable.Values(object[][] rows) =>
        (IInsertIgnoreBuilderValues)Values(rows);

    IInsertIgnoreBuilderValues IInsertIgnoreBuilderValues.Values(params object[] values) =>
        (IInsertIgnoreBuilderValues)Values(values);

    // The DO UPDATE SET WHERE filter. Explicit implementation keeps this distinct
    // from the inherited SelectBuilder.Where (which returns a SELECT builder);
    // both add the same WhereClause, but this preserves the UPSERT chain.
    IReturning IInsertBuilderDoUpdateSet.Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    public ISelectBuilder With(params CommonTableExpression[] ctes)
    {
        AddPart(new WithClause(ctes));
        return this;
    }

    public ISelectBuilder WithRecursive(params CommonTableExpression[] ctes)
    {
        AddPart(new WithRecursiveClause(ctes));
        return this;
    }

    // #397's width class, extended to INSERT ... SELECT where the select list's
    // width is knowable (a star item's width is the schema's). Only the first block
    // feeds the column list; a set operator's later blocks are the engine's to width.
    private protected override void OnSelectItems(SqlPart[] selectItems)
    {
        if (columnCount == 0 || FirstSelectItems() is not null)
        {
            return;
        }

        foreach (SqlPart item in selectItems)
        {
            if (item is AsteriskMarker or QualifiedAsteriskMarker)
            {
                return;
            }
        }

        if (selectItems.Length != columnCount)
        {
            throw new ArgumentException(
                $"The INSERT column list declares {columnCount} column(s), "
                + $"but the SELECT list has {selectItems.Length} item(s).");
        }
    }

    protected override void Validate(Dbms dbms)
    {
        // The SELECT guards still apply to the INSERT ... SELECT chain, which
        // inherits the whole SELECT surface.
        base.Validate(dbms);

        OnDuplicateKeyUpdateClause? onDuplicateKeyUpdate = FindPart<OnDuplicateKeyUpdateClause>();
        DmlTargetGuard.ThrowIfLeadingWithBeforeOnDuplicateKeyUpdate(
            PartsSpan, onDuplicateKeyUpdate);
        DmlTargetGuard.ThrowIfLeadingWithUnsupported(PartsSpan, dbms, insert: true);
        DmlTargetGuard.ThrowIfAliasedOnSqlServer(table, dbms);
        DmlTargetGuard.ThrowIfInsertTargetAliasedOnMySql(table, dbms);

        OnConflictClause? onConflict = FindPart<OnConflictClause>();
        if (dbms == Dbms.PostgreSql
            && onConflict is { HasTarget: false }
            && FindPart<DoUpdateSetClause>() is not null)
        {
            throw new ArgumentException(
                "PostgreSQL requires a conflict target for ON CONFLICT DO UPDATE; "
                    + "name the column(s) in OnConflict(...).");
        }

        OutputClause? output = FindPart<OutputClause>();
        OutputClauseGuard.ThrowIfCombinedWithReturning(
            output, FindPart<ReturningClause>(), FindPart<ReturningIntoClause>());
        OutputClauseGuard.ThrowIfInsertCombinedWithUpsert(output, onConflict, onDuplicateKeyUpdate);
        ReturningGuard.ThrowIfCombinedWithMySqlInsertForm(
            FindPart<InsertIgnoreIntoClause>(),
            onDuplicateKeyUpdate,
            FindPart<ReturningClause>(),
            FindPart<ReturningIntoClause>());
    }

    // Resolve and width-check the whole batch before touching builder state: a
    // throw on a later row would otherwise leave the earlier rows appended, and
    // the supported fix-up retry on the same instance would insert them twice.
    private void AddValuesRows(IEnumerable<object[]> rows)
    {
        List<SqlExpression[]> resolved = [];
        int expectedWidth = _valuesClause?.RowWidth ?? 0;

        foreach (object[] row in rows)
        {
            if (row is null)
            {
                throw new ArgumentNullException(
                    nameof(rows), "A VALUES source must not contain a null row.");
            }

            SqlExpression[] resolvedRow = InsertValueResolver.Resolve(row);
            if (expectedWidth == 0)
            {
                if (columnCount > 0 && resolvedRow.Length != columnCount)
                {
                    throw new ArgumentException(
                        $"The INSERT column list declares {columnCount} column(s), "
                        + $"but this VALUES row has {resolvedRow.Length} value(s).");
                }

                expectedWidth = resolvedRow.Length;
            }
            else if (resolvedRow.Length != expectedWidth)
            {
                throw new ArgumentException(
                    "All rows in a multi-row INSERT must have the same number of values; "
                    + $"the first row has {expectedWidth}, but this row has {resolvedRow.Length}.");
            }

            resolved.Add(resolvedRow);
        }

        if (resolved.Count == 0)
        {
            throw new ArgumentException(NoRowsMessage);
        }

        foreach (SqlExpression[] row in resolved)
        {
            if (_valuesClause is null)
            {
                _valuesClause = InsertValuesClause.FromResolved(row);
                AddPart(_valuesClause);
            }
            else
            {
                _valuesClause.AddResolvedRow(row);
            }
        }
    }

    // A repeat Values(...) call grows the held clause (AddRow checks its width)
    // rather than adding a second VALUES part.
    private void AddValuesRow(object[] values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(
                nameof(values), "A VALUES source must not contain a null row.");
        }

        if (_valuesClause is null)
        {
            if (columnCount > 0 && values.Length > 0 && values.Length != columnCount)
            {
                throw new ArgumentException(
                    $"The INSERT column list declares {columnCount} column(s), "
                    + $"but this VALUES row has {values.Length} value(s).");
            }

            _valuesClause = InsertValuesClause.Parse(values);
            AddPart(_valuesClause);
        }
        else
        {
            _valuesClause.AddRow(values);
        }
    }
}
