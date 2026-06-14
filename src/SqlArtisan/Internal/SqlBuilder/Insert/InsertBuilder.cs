namespace SqlArtisan.Internal;

internal sealed class InsertBuilder(params SqlPart[] rootParts) :
    SelectBuilder(rootParts),
    IInsertBuilderColumns,
    IInsertBuilderSet,
    IInsertBuilderTable,
    IInsertBuilderValues,
    IOnConflictBuilder
{
    private InsertValuesClause? _valuesClause;
    private DbColumn[] _conflictTarget = [];

    public IOnConflictBuilder OnConflict(params DbColumn[] conflictTarget)
    {
        _conflictTarget = conflictTarget;
        return this;
    }

    public ISqlBuilder DoNothing()
    {
        AddPart(new OnConflictClause(_conflictTarget, updateColumns: null));
        return this;
    }

    public ISqlBuilder DoUpdateSet(params DbColumn[] updateColumns)
    {
        AddPart(new OnConflictClause(_conflictTarget, updateColumns));
        return this;
    }

    public ISqlBuilder OnDuplicateKeyUpdate(params DbColumn[] updateColumns)
    {
        AddPart(new OnDuplicateKeyUpdateClause(updateColumns));
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
