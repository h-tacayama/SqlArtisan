namespace SqlArtisan;
internal sealed class InsertBuilder :
    SelectBuilder,
    IInsertBuilderInsertInto,
    IInsertBuilderSelect,
    IInsertBuilderSet
{
    internal InsertBuilder(InsertIntoClause insertIntoClause)
        : base(insertIntoClause)
    {
    }

    internal InsertBuilder(InsertSelectClause insertSelectClause)
        : base(insertSelectClause)
    {
    }

    public IInsertBuilderSet Set(params EqualityBasedCondition[] assignments)
    {
        AddPart(InsertSetClause.Parse(assignments));
        return this;
    }
}
