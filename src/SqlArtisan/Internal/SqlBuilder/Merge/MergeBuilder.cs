namespace SqlArtisan.Internal;

internal sealed class MergeBuilder(params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    IMergeBuilderOn,
    IMergeBuilderTarget,
    IMergeBuilderThenInsert,
    IMergeBuilderThenUpdateSet,
    IMergeBuilderUsing,
    IMergeBuilderWhen,
    IMergeBuilderWhenMatched,
    IMergeBuilderWhenNotMatched,
    IMergeBuilderWhenNotMatchedBySource
{
    // The column count of the most recent ThenInsert, cross-checked by the next
    // Values call — the same #397 width guard plain INSERT threads through its
    // constructor; MERGE's fluent pairing makes a field the equivalent carrier.
    private int _pendingInsertColumnCount;

    protected override string StatementName => Keywords.Merge;

    public SqlStatement Build() => BuildCore(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) => BuildCore(dbms);

    public IMergeBuilderWhen DeleteWhere(SqlCondition condition)
    {
        AddPart(new MergeDeleteWhereClause(condition));
        return this;
    }

    public IMergeBuilderOn On(SqlCondition condition)
    {
        AddPart(new MergeOnClause(condition));
        return this;
    }

    // Shared by IMergeBuilderWhenMatched and IMergeBuilderWhenNotMatchedBySource
    // (same signature and return type), so one implementation satisfies both.
    public IMergeBuilderWhen ThenDelete()
    {
        AddPart(new MergeDeleteClause());
        return this;
    }

    public IMergeBuilderThenInsert ThenInsert()
    {
        _pendingInsertColumnCount = 0;
        AddPart(new MergeInsertClause([]));
        return this;
    }

    public IMergeBuilderThenInsert ThenInsert(params DbColumn[] columns)
    {
        CollectionGuard.ThrowIfEmpty(columns, "An INSERT column list requires at least one column.");
        CollectionGuard.ThrowIfNullElement(
            columns, nameof(columns), "An INSERT column list must not contain a null column.");

        _pendingInsertColumnCount = columns.Length;
        AddPart(new MergeInsertClause(columns));
        return this;
    }

    // ThenUpdateSet differs only by return type between the two branch interfaces,
    // so each is implemented explicitly.
    IMergeBuilderThenUpdateSet IMergeBuilderWhenMatched.ThenUpdateSet(
        params EqualityCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments));
        return this;
    }

    IMergeBuilderWhen IMergeBuilderWhenNotMatchedBySource.ThenUpdateSet(
        params EqualityCondition[] assignments)
    {
        AddPart(MergeUpdateSetClause.Parse(assignments));
        return this;
    }

    public IMergeBuilderUsing Using(TableReference source)
    {
        AddPart(new MergeUsingClause(source));
        return this;
    }

    public IMergeBuilderWhen Values(params object[] values)
    {
        if (_pendingInsertColumnCount > 0
            && values.Length > 0
            && values.Length != _pendingInsertColumnCount)
        {
            throw new ArgumentException(
                $"The INSERT column list declares {_pendingInsertColumnCount} column(s), " +
                $"but this VALUES row has {values.Length} value(s).");
        }

        AddPart(InsertValuesClause.Parse(values));
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

    // SQL Server requires a MERGE to end in a semicolon; the dialect supplies it
    // (empty for every other DBMS, leaving their output unchanged).
    protected override void AppendTrailing(SqlBuildingBuffer buffer) =>
        buffer.AppendMergeTerminator();
}
