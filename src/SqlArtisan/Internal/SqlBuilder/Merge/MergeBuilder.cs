namespace SqlArtisan.Internal;

// Interfaces and members are ordered to follow the fluent construction flow
// (MERGE INTO -> USING -> ON -> WHEN branches), so the class reads top-to-bottom
// in the order a caller uses it and each WHEN branch sits next to its actions.
internal sealed class MergeBuilder(params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IMergeBuilderTarget,
    IMergeBuilderUsing,
    IMergeBuilderOn,
    IMergeBuilderWhenMatched,
    IMergeBuilderThenUpdateSet,
    IMergeBuilderWhenNotMatched,
    IMergeBuilderThenInsert,
    IMergeBuilderWhenNotMatchedBySource
{
    public SqlStatement Build() => BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) => BuildCore(dbms);

    public IMergeBuilderUsing Using(TableReference source)
    {
        AddPart(new MergeUsingClause(source));
        return this;
    }

    public IMergeBuilderOn On(SqlCondition condition)
    {
        AddPart(new MergeOnClause(condition));
        return this;
    }

    public IMergeBuilderWhenMatched WhenMatched()
    {
        AddPart(new WhenMatchedClause(null));
        return this;
    }

    public IMergeBuilderWhenMatched WhenMatched(SqlCondition extraCondition)
    {
        AddPart(new WhenMatchedClause(extraCondition));
        return this;
    }

    // ThenUpdateSet differs only by return type between the two branch interfaces,
    // so each is implemented explicitly (here for the WHEN MATCHED branch).
    IMergeBuilderThenUpdateSet IMergeBuilderWhenMatched.ThenUpdateSet(
        params EqualityBasedCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments));
        return this;
    }

    public IMergeBuilderOn DeleteWhere(SqlCondition condition)
    {
        AddPart(new MergeDeleteWhereClause(condition));
        return this;
    }

    // Shared by IMergeBuilderWhenMatched and IMergeBuilderWhenNotMatchedBySource
    // (same signature and return type), so one implementation satisfies both.
    public IMergeBuilderOn ThenDelete()
    {
        AddPart(new MergeDeleteClause());
        return this;
    }

    public IMergeBuilderWhenNotMatched WhenNotMatched()
    {
        AddPart(new WhenNotMatchedClause(null));
        return this;
    }

    public IMergeBuilderWhenNotMatched WhenNotMatched(SqlCondition extraCondition)
    {
        AddPart(new WhenNotMatchedClause(extraCondition));
        return this;
    }

    public IMergeBuilderThenInsert ThenInsert(params DbColumn[] columns)
    {
        AddPart(new MergeInsertClause(columns));
        return this;
    }

    public IMergeBuilderOn Values(params object[] values)
    {
        AddPart(InsertValuesClause.Parse(values));
        return this;
    }

    public IMergeBuilderWhenNotMatchedBySource WhenNotMatchedBySource()
    {
        AddPart(new WhenNotMatchedBySourceClause(null));
        return this;
    }

    public IMergeBuilderWhenNotMatchedBySource WhenNotMatchedBySource(SqlCondition extraCondition)
    {
        AddPart(new WhenNotMatchedBySourceClause(extraCondition));
        return this;
    }

    IMergeBuilderOn IMergeBuilderWhenNotMatchedBySource.ThenUpdateSet(
        params EqualityBasedCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments));
        return this;
    }

    // SQL Server requires a MERGE to end in a semicolon; the dialect supplies it
    // (empty for every other DBMS, leaving their output unchanged).
    protected override void AppendTrailing(SqlBuildingBuffer buffer) =>
        buffer.AppendMergeTerminator();
}
