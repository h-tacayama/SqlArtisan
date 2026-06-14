using SqlArtisan.Internal;

namespace SqlArtisan.Databases.SqlServer;

// SQL Server mirror of Oracle.Merge.cs: identical state machine, different
// return types, zero reuse. The trailing semicolon that SQL Server's MERGE
// requires is applied in the shared MergeBuilder via the dialect — the wrappers
// only narrow reachability, exactly as in the Oracle file.
public static partial class Sql
{
    public static SqlServerMergeUsing MergeInto(DbTableBase target) =>
        new(global::SqlArtisan.Sql.MergeInto(target));
}

public sealed class SqlServerMergeUsing
{
    private readonly IMergeBuilderUsing _inner;

    internal SqlServerMergeUsing(IMergeBuilderUsing inner) => _inner = inner;

    public SqlServerMergeOn Using(TableReference source) => new(_inner.Using(source));
}

public sealed class SqlServerMergeOn
{
    private readonly IMergeBuilderOn _inner;

    internal SqlServerMergeOn(IMergeBuilderOn inner) => _inner = inner;

    public SqlServerMergeWhen On(SqlCondition condition) => new(_inner.On(condition));
}

public sealed class SqlServerMergeWhen
{
    private readonly IMergeBuilderWhen _inner;

    internal SqlServerMergeWhen(IMergeBuilderWhen inner) => _inner = inner;

    public SqlServerMergeWhen WhenMatchedThenUpdateSet(params EqualityBasedCondition[] assignments) =>
        new(_inner.WhenMatchedThenUpdateSet(assignments));

    public SqlServerMergeInsert WhenNotMatchedThenInsert(params DbColumn[] columns) =>
        new(_inner.WhenNotMatchedThenInsert(columns));
}

public sealed class SqlServerMergeInsert
{
    private readonly IMergeInsertColumns _inner;

    internal SqlServerMergeInsert(IMergeInsertColumns inner) => _inner = inner;

    public SqlServerQuery Values(params object[] values) => new(_inner.Values(values));
}
