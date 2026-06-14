using SqlArtisan.Internal;

namespace SqlArtisan.Databases.Oracle;

// ─────────────────────────────────────────────────────────────────────────────
// MERGE through the Oracle namespace (Approach B, clause level — second sample).
// MERGE is a full statement, not an INSERT tail, so its fluent chain is deeper
// than UPSERT's: five states → five wrapper types, here, and five more for SQL
// Server. None are reusable across the two namespaces (return types differ), and
// none are generated. This file plus SqlServer.Merge.cs is the second data point
// for the clause-level maintenance-cost estimate (see the spike README).
// ─────────────────────────────────────────────────────────────────────────────
public static partial class Sql
{
    public static OracleMergeUsing MergeInto(DbTableBase target) =>
        new(global::SqlArtisan.Sql.MergeInto(target));
}

public sealed class OracleMergeUsing
{
    private readonly IMergeBuilderUsing _inner;

    internal OracleMergeUsing(IMergeBuilderUsing inner) => _inner = inner;

    public OracleMergeOn Using(TableReference source) => new(_inner.Using(source));
}

public sealed class OracleMergeOn
{
    private readonly IMergeBuilderOn _inner;

    internal OracleMergeOn(IMergeBuilderOn inner) => _inner = inner;

    public OracleMergeWhen On(SqlCondition condition) => new(_inner.On(condition));
}

public sealed class OracleMergeWhen
{
    private readonly IMergeBuilderWhen _inner;

    internal OracleMergeWhen(IMergeBuilderWhen inner) => _inner = inner;

    public OracleMergeWhen WhenMatchedThenUpdateSet(params EqualityBasedCondition[] assignments) =>
        new(_inner.WhenMatchedThenUpdateSet(assignments));

    public OracleMergeInsert WhenNotMatchedThenInsert(params DbColumn[] columns) =>
        new(_inner.WhenNotMatchedThenInsert(columns));
}

public sealed class OracleMergeInsert
{
    private readonly IMergeInsertColumns _inner;

    internal OracleMergeInsert(IMergeInsertColumns inner) => _inner = inner;

    public OracleQuery Values(params object[] values) => new(_inner.Values(values));
}
