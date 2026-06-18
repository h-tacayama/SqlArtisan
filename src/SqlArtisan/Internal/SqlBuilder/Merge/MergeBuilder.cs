namespace SqlArtisan.Internal;

internal sealed class MergeBuilder(params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IMergeBuilderUsing,
    IMergeBuilderOn,
    IMergeBuilderWhen,
    IMergeMatchedThen,
    IMergeMatchedUpdateAction,
    IMergeNotMatchedThen,
    IMergeInsertValues,
    IMergeNotMatchedBySourceThen
{
    public SqlStatement Build() => BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) => BuildCore(dbms);

    public IMergeBuilderOn Using(TableReference source)
    {
        AddPart(new MergeUsingClause(source));
        return this;
    }

    public IMergeBuilderWhen On(SqlCondition condition)
    {
        AddPart(new MergeOnClause(condition));
        return this;
    }

    public IMergeMatchedThen WhenMatched()
    {
        AddPart(new WhenMatchedClause(null));
        return this;
    }

    public IMergeMatchedThen WhenMatched(SqlCondition extraCondition)
    {
        AddPart(new WhenMatchedClause(extraCondition));
        return this;
    }

    public IMergeNotMatchedThen WhenNotMatched()
    {
        AddPart(new WhenNotMatchedClause(null));
        return this;
    }

    public IMergeNotMatchedThen WhenNotMatched(SqlCondition extraCondition)
    {
        AddPart(new WhenNotMatchedClause(extraCondition));
        return this;
    }

    public IMergeNotMatchedBySourceThen WhenNotMatchedBySource()
    {
        AddPart(new WhenNotMatchedBySourceClause(null));
        return this;
    }

    public IMergeNotMatchedBySourceThen WhenNotMatchedBySource(SqlCondition extraCondition)
    {
        AddPart(new WhenNotMatchedBySourceClause(extraCondition));
        return this;
    }

    public IMergeBuilderWhen DeleteWhere(SqlCondition condition)
    {
        AddPart(new MergeDeleteWhereClause(condition));
        return this;
    }

    public IMergeInsertValues ThenInsert(params DbColumn[] columns)
    {
        AddPart(new MergeInsertClause(columns));
        return this;
    }

    public IMergeBuilderWhen Values(params object[] values)
    {
        AddPart(InsertValuesClause.Parse(values));
        return this;
    }

    // Shared by IMergeMatchedThen and IMergeNotMatchedBySourceThen (same signature
    // and return type), so one implementation satisfies both.
    public IMergeBuilderWhen ThenDelete()
    {
        AddPart(new MergeDeleteClause());
        return this;
    }

    // ThenUpdateSet differs only by return type between the two branch interfaces,
    // so each is implemented explicitly.
    IMergeMatchedUpdateAction IMergeMatchedThen.ThenUpdateSet(
        params EqualityBasedCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments));
        return this;
    }

    IMergeBuilderWhen IMergeNotMatchedBySourceThen.ThenUpdateSet(
        params EqualityBasedCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments));
        return this;
    }

    // SQL Server requires a MERGE to end in a semicolon; the dialect supplies it
    // (empty for every other DBMS, leaving their output unchanged).
    protected override void AppendTrailing(SqlBuildingBuffer buffer) =>
        buffer.AppendStatementTerminator();
}
