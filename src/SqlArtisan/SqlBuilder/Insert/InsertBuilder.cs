namespace SqlArtisan;
internal sealed class InsertBuilder :
    SelectBuilder,
    IInsertBuilderInsertInto,
    IInsertBuilderValues
{
    internal InsertBuilder(InsertIntoClause insertIntoClause)
        : base(insertIntoClause)
    {
    }

    public IInsertBuilderValues Values(params object[] values)
    {
        AddPart(InsertValuesClause.Parse(values));
        return this;
    }
}
