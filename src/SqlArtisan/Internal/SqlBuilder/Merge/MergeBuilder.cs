namespace SqlArtisan.Internal;

internal sealed class MergeBuilder(params SqlPart[] rootParts) :
    SqlBuilderBase(rootParts),
    ISqlBuilder,
    IMergeBuilderUsing,
    IMergeBuilderOn,
    IMergeBuilderWhen,
    IMergeInsertColumns
{
    public SqlStatement Build() => BuildMerge(SqlArtisanConfig.DefaultDbms);

    public SqlStatement Build(Dbms dbms) => BuildMerge(dbms);

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

    public IMergeBuilderWhen WhenMatchedThenUpdateSet(params EqualityBasedCondition[] assignments)
    {
        var equalities = new EqualityCondition[assignments.Length];

        for (int i = 0; i < assignments.Length; i++)
        {
            equalities[i] = assignments[i] as EqualityCondition
                ?? throw new ArgumentException(
                    $"Invalid type for EqualityCondition: {assignments[i].GetType()}");
        }

        AddPart(new WhenMatchedUpdateClause(equalities));
        return this;
    }

    public IMergeInsertColumns WhenNotMatchedThenInsert(params DbColumn[] columns)
    {
        _insertColumns = columns;
        return this;
    }

    public ISqlBuilder Values(params object[] values)
    {
        AddPart(new WhenNotMatchedInsertClause(_insertColumns, InsertValueResolver.Resolve(values)));
        return this;
    }

    private DbColumn[] _insertColumns = [];

    // MERGE cannot reuse SqlBuilderBase.BuildCore directly: SQL Server's trailing
    // ";" must abut the final clause with no space, so the space-separated parts
    // are formatted first, then the dialect terminator is appended tightly.
    private SqlStatement BuildMerge(Dbms dbms)
    {
        IDbmsDialect dialect = DbmsDialectFactory.Create(dbms);
        using SqlBuildingBuffer buffer = new(dialect);
        FormatCore(buffer);
        buffer.AppendStatementTerminator();
        return buffer.ToSqlStatement();
    }
}
