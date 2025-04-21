namespace InlineSqlSharp;
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

    public IInsertBuilderSet SET(params AbstractEqualityCondition[] assignments)
    {
        AddElement(InsertSetClause.Parse(assignments));
        return this;
    }
}
