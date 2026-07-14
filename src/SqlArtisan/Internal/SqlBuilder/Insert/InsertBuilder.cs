namespace SqlArtisan.Internal;

internal sealed class InsertBuilder(DbTableBase table, params SqlPart[] rootParts) :
    SelectBuilder(rootParts),
    IInsertBuilderColumns,
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
    private InsertValuesClause? _valuesClause;

    protected override string StatementName => Keywords.Insert;

    public IReturning DoNothing()
    {
        AddPart(new DoNothingClause());
        return this;
    }

    public IInsertBuilderDoUpdateSet DoUpdateSet(params EqualityBasedCondition[] assignments)
    {
        AddPart(DoUpdateSetClause.Parse(assignments));
        return this;
    }

    public IInsertBuilderOnConflict OnConflict(params DbColumn[] conflictTarget)
    {
        AddPart(new OnConflictClause(conflictTarget));
        return this;
    }

    public IReturning OnDuplicateKeyUpdate(params EqualityBasedCondition[] assignments)
    {
        AddPart(new RowAliasClause());
        AddPart(OnDuplicateKeyUpdateClause.Parse(assignments));
        return this;
    }

    public IReturningBuilder Returning(params object[] expressions) =>
        ReturningBuilder.Create(this, expressions);

    public IInsertBuilderSet Set(params EqualityBasedCondition[] assignments)
    {
        AddPart(InsertSetClause.Parse(assignments));
        return this;
    }

    // The narrowed INSERT IGNORE chain reuses the same builder; only the static
    // return type drops IUpsert, so ON CONFLICT / ON DUPLICATE KEY UPDATE can't
    // be chained after INSERT IGNORE (ODKU would override IGNORE — nonsense SQL).
    IInsertIgnoreBuilderSet IInsertIgnoreBuilderTable.Set(params EqualityBasedCondition[] assignments) =>
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

        bool any = false;
        foreach (object[] row in rows)
        {
            AddValuesRow(row);
            any = true;
        }

        if (!any)
        {
            throw new ArgumentException(
                "VALUES requires at least one row; the row collection is empty.");
        }

        return this;
    }

    public IInsertBuilderValues Values(object[][] rows) =>
        Values((IEnumerable<object[]>)rows);

    // The single-row append shared by every Values overload. A repeat call grows
    // the held clause via AddRow (which validates row width), bypassing AddPart's
    // once-per-part guard.
    private void AddValuesRow(object[] values)
    {
        if (_valuesClause is null)
        {
            _valuesClause = InsertValuesClause.Parse(values);
            AddPart(_valuesClause);
        }
        else
        {
            _valuesClause.AddRow(values);
        }
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

    protected override void Validate(Dbms dbms) =>
        DmlTargetGuard.ThrowIfAliasedOnSqlServer(table, dbms);
}
