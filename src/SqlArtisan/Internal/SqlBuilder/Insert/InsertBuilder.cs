namespace SqlArtisan.Internal;

internal sealed class InsertBuilder(params SqlPart[] rootParts) :
    SelectBuilder(rootParts),
    IInsertBuilderColumns,
    IInsertBuilderSet,
    IInsertBuilderTable,
    IInsertBuilderValues,
    IInsertBuilderOnConflict,
    IInsertBuilderDoUpdateSet
{
    private InsertValuesClause? _valuesClause;

    public IReadOnlyList<SqlStatement> BuildBatches() =>
        BuildBatches(SqlArtisanConfig.DefaultDbms);

    public IReadOnlyList<SqlStatement> BuildBatches(Dbms dbms)
    {
        IDbmsDialect dialect = DbmsDialectFactory.Create(dbms);

        if (!dialect.SupportsMultiRowValues)
        {
            throw new NotSupportedException(
                $"{dbms} does not support multi-row VALUES, so batched bulk INSERT " +
                "is not available. Use array binding (bulk copy) instead.");
        }

        if (_valuesClause is null)
        {
            throw new InvalidOperationException(
                "BuildBatches requires at least one Values(...) row.");
        }

        int columnsPerRow = _valuesClause.ColumnCount;

        if (columnsPerRow == 0)
        {
            throw new InvalidOperationException(
                "BuildBatches requires each row to have at least one value.");
        }

        int maxParameters = dialect.MaxParameters;

        if (columnsPerRow > maxParameters)
        {
            throw new InvalidOperationException(
                $"A single row carries {columnsPerRow} parameters, which exceeds the " +
                $"{dbms} limit of {maxParameters}; it cannot be batched.");
        }

        int rowCount = _valuesClause.RowCount;
        int rowsPerBatch = maxParameters / columnsPerRow;
        var batches = new List<SqlStatement>((rowCount + rowsPerBatch - 1) / rowsPerBatch);

        try
        {
            for (int offset = 0; offset < rowCount; offset += rowsPerBatch)
            {
                int length = Math.Min(rowsPerBatch, rowCount - offset);
                _valuesClause.SetWindow(offset, length);
                batches.Add(BuildCore(dbms));
            }
        }
        finally
        {
            _valuesClause.ClearWindow();
        }

        return batches;
    }

    public IInsertBuilderOnConflict OnConflict(params DbColumn[] conflictTarget)
    {
        AddPart(new OnConflictClause(conflictTarget));
        return this;
    }

    public IInsertReturning DoNothing()
    {
        AddPart(new DoNothingClause());
        return this;
    }

    public IInsertBuilderDoUpdateSet DoUpdateSet(params EqualityBasedCondition[] assignments)
    {
        AddPart(DoUpdateSetClause.Parse(assignments));
        return this;
    }

    public IInsertReturning OnDuplicateKeyUpdate(params EqualityBasedCondition[] assignments)
    {
        AddPart(new RowAliasClause());
        AddPart(OnDuplicateKeyUpdateClause.Parse(assignments));
        return this;
    }

    // The DO UPDATE SET WHERE filter. Explicit implementation keeps this distinct
    // from the inherited SelectBuilder.Where (which returns a SELECT builder);
    // both add the same WhereClause, but this preserves the UPSERT chain.
    IInsertReturning IInsertBuilderDoUpdateSet.Where(SqlCondition condition)
    {
        AddPart(new WhereClause(condition));
        return this;
    }

    public IReturningBuilder Returning(params object[] expressions) =>
        ReturningBuilder.Create(this, expressions);

    public IInsertBuilderSet Set(params EqualityBasedCondition[] assignments)
    {
        AddPart(InsertSetClause.Parse(assignments));
        return this;
    }

    public IInsertBuilderValues Values(params object[] values)
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
}
