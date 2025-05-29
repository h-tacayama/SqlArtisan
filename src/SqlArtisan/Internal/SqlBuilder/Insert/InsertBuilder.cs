namespace SqlArtisan.Internal;

internal sealed class InsertBuilder(InsertIntoClause insertIntoClause) :
    SelectBuilder(insertIntoClause),
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
}
