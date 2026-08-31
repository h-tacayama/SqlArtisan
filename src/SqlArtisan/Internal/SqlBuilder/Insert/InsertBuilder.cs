namespace SqlArtisan.Internal;

internal sealed class InsertBuilder(DbTableBase table, int columnCount, params SqlPart[] rootParts) :
    SelectBuilder(rootParts),
    IInsertBuilderColumns,
    IInsertBuilderColumnsOutput,
    IInsertBuilderColumnsOutputInto,
    IInsertBuilderDoUpdateSet,
    IInsertBuilderOnConflict,
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
        AddPart(new OutputIntoClause(table, columns));
        return this;
    }

    public IInsertBuilderOnConflict OnConflict(params DbColumn[] conflictTarget)
    {
        AddPart(new OnConflictClause(conflictTarget));
        return this;
    }

    public IReturning OnDuplicateKeyUpdate(params EqualityCondition[] assignments)
    {
        AddPart(new RowAliasClause());
        AddPart(OnDuplicateKeyUpdateClause.Parse(assignments));
        return this;
    }

    public IInsertBuilderColumnsOutputInto Output(params object[] items)
    {
        CollectionGuard.ThrowIfEmpty(
            items, nameof(items), "OUTPUT requires at least one expression.");
        AddPart(new OutputClause(SelectItemResolver.Resolve(items)));
        return this;
    }

    public IReturningBuilder Returning(params object[] expressions) =>
        ReturningBuilder.Create(this, expressions);

    public IInsertBuilderSet Set(params EqualityCondition[] assignments)
    {
        AddPart(InsertSetClause.Parse(assignments));
        return this;
    }

    // The narrowed INSERT IGNORE chain reuses the same builder; only the static
    // return type drops IUpsert, so ON CONFLICT / ON DUPLICATE KEY UPDATE can't
    // be chained after INSERT IGNORE (ODKU would override IGNORE — nonsense SQL).
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

    protected override void Validate(Dbms dbms)
    {
        // The base TOP guards still apply to the INSERT ... SELECT chain, which
        // inherits the whole SELECT surface.
        base.Validate(dbms);

        DmlTargetGuard.ThrowIfAliasedOnSqlServer(table, dbms);
        DmlTargetGuard.ThrowIfInsertTargetAliasedOnMySql(table, dbms);

        // #397's width class, extended to INSERT ... SELECT where the select
        // list's width is knowable (a star item's width is the schema's).
        SqlPart[]? selectItems = FirstSelectItems();
        if (columnCount > 0 && selectItems is not null)
        {
            bool countable = true;
            foreach (SqlPart item in selectItems)
            {
                if (item is AsteriskMarker or QualifiedAsteriskMarker)
                {
                    countable = false;
                    break;
                }
            }

            if (countable && selectItems.Length != columnCount)
            {
                throw new ArgumentException(
                    $"The INSERT column list declares {columnCount} column(s), " +
                    $"but the SELECT list has {selectItems.Length} item(s).");
            }
        }

        OnConflictClause? onConflict = FindPart<OnConflictClause>();
        if (onConflict is { HasTarget: false } && FindPart<DoUpdateSetClause>() is not null)
        {
            throw new ArgumentException(
                "ON CONFLICT DO UPDATE requires a conflict target; name the column(s) in OnConflict(...).");
        }

        OutputClause? output = FindPart<OutputClause>();
        OutputClauseGuard.ThrowIfCombinedWithReturning(
            output, FindPart<ReturningClause>(), FindPart<ReturningIntoClause>());
        OutputClauseGuard.ThrowIfInsertCombinedWithUpsert(
            output, onConflict, FindPart<OnDuplicateKeyUpdateClause>());
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
                        $"The INSERT column list declares {columnCount} column(s), " +
                        $"but this VALUES row has {resolvedRow.Length} value(s).");
                }

                expectedWidth = resolvedRow.Length;
            }
            else if (resolvedRow.Length != expectedWidth)
            {
                throw new ArgumentException(
                    "All rows in a multi-row INSERT must have the same number of values; " +
                    $"the first row has {expectedWidth}, but this row has {resolvedRow.Length}.");
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

    // The single-row append shared by every Values overload. A repeat call grows
    // the held clause via AddRow (which validates row width), bypassing AddPart's
    // once-per-part guard.
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
                    $"The INSERT column list declares {columnCount} column(s), " +
                    $"but this VALUES row has {values.Length} value(s).");
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
