namespace SqlArtisan.Internal;

internal sealed class InsertBuilder(params SqlPart[] rootParts) :
    SelectBuilder(rootParts),
    IInsertBuilderColumns,
    IInsertBuilderSet,
    IInsertBuilderTable,
    IInsertBuilderValues
{
    public IInsertBuilderSet Set(params EqualityBasedCondition[] assignments)
    {
        AddPart(InsertSetClause.Parse(assignments));
        return this;
    }

    public IInsertBuilderValues Values(params object[] values)
    {
        AddPart(InsertValuesClause.Parse(values));
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
