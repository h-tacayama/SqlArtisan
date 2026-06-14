using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Oracle;

// ─────────────────────────────────────────────────────────────────────────────
// Thin slice of the ③ direction wired into the real library: a per-DBMS facade
// exposing only Oracle's numeric syntax. `using SqlArtisan.Oracle;` surfaces
// Ceil (no Ceiling) in IntelliSense; the DBMS is folded in so Build() needs no
// argument; nodes are tagged Dbms.Oracle so reuse in another DBMS's build is
// caught (see SqlBuildingBuffer.Append).
//
// Only Abs/Ceil are sliced here to validate integration without breaking the
// existing test suite. The full surface would be code-generated from the
// catalog (see /spikes/dbms-namespace).
// ─────────────────────────────────────────────────────────────────────────────
public static class Sql
{
    /// <summary>Starts a SELECT bound to Oracle; <see cref="OracleQuery.Build"/>
    /// takes no DBMS argument.</summary>
    public static OracleQuery Select(params object[] selectItems) =>
        new((ISqlBuilder)SqlArtisan.Sql.Select(selectItems));

    public static AbsFunction Abs(object expr) =>
        new(Resolve(expr), Dbms.Oracle);

    /// <summary>The <c>CEIL(expr)</c> function. Oracle has no <c>CEILING</c>.</summary>
    public static CeilFunction Ceil(object expr) =>
        new(Resolve(expr), Dbms.Oracle);
}

/// <summary>A query whose build target is fixed to Oracle.</summary>
public sealed class OracleQuery
{
    private readonly ISqlBuilder _inner;

    internal OracleQuery(ISqlBuilder inner) => _inner = inner;

    public SqlStatement Build() => _inner.Build(Dbms.Oracle);
}
