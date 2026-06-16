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

    public IInsertBuilderOnConflict OnConflict(params DbColumn[] conflictTarget)
    {
        AddPart(new OnConflictClause(conflictTarget));
        return this;
    }

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

    public IReturning OnDuplicateKeyUpdate(params EqualityBasedCondition[] assignments)
    {
        AddPart(new RowAliasClause());
        AddPart(OnDuplicateKeyUpdateClause.Parse(assignments));
        return this;
    }

    // The DO UPDATE SET WHERE filter. Explicit implementation keeps this distinct
    // from the inherited SelectBuilder.Where (which returns a SELECT builder);
    // both add the same WhereClause, but this preserves the UPSERT chain.
    IReturning IInsertBuilderDoUpdateSet.Where(SqlCondition condition)
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
