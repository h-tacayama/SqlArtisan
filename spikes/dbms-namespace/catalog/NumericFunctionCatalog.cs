// ─────────────────────────────────────────────────────────────────────────────
// SPIKE / ILLUSTRATIVE ONLY — not compiled by any project, not wired into the
// solution. This is the "single source of truth" the design depends on: every
// numeric function declared once, with its per-DBMS availability and spelling.
//
// A generator (source generator or T4 — out of scope for this spike) would read
// this catalog and emit one `Sql` facade per `Dbms` (see ../generated/*.g.cs),
// emitting only the methods whose entry includes that `Dbms`.
//
// ⚠️ The availability data below is AI-drafted and MUST be verified against each
// vendor's documentation before it is trusted. See ../README.md §1.
// ─────────────────────────────────────────────────────────────────────────────

using SqlArtisan;

namespace SqlArtisan.Spike.Catalog;

/// <summary>One function's identity, arity, and per-DBMS spelling.</summary>
/// <param name="Name">The C# factory name SqlArtisan exposes (e.g. "Ceil").</param>
/// <param name="Arity">Number of arguments (overloads listed separately).</param>
/// <param name="Spelling">
/// DBMS → emitted SQL token. A DBMS absent from this map means the function is
/// NOT generated for that DBMS's namespace (it does not exist there, or is only
/// reachable via an operator / a different construct).
/// </param>
internal sealed record FunctionSpec(
    string Name,
    int Arity,
    IReadOnlyDictionary<Dbms, string> Spelling)
{
    /// <summary>The DBMS this function is valid on — drives both generation and
    /// the node's authoring tag (see ../prototype/DbmsAffinity.cs).</summary>
    public IEnumerable<Dbms> SupportedOn => Spelling.Keys;

    public bool IsSupportedOn(Dbms dbms) => Spelling.ContainsKey(dbms);
}

internal static class NumericFunctionCatalog
{
    private static readonly Dbms[] s_all =
    {
        Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite, Dbms.SqlServer,
    };

    /// <summary>Same token on every DBMS — the universal case.</summary>
    private static FunctionSpec Universal(string name, string token, int arity) =>
        new(name, arity, s_all.ToDictionary(d => d, _ => token));

    /// <summary>Available only on the listed DBMS, each with its own token.</summary>
    private static FunctionSpec Diverging(
        string name, int arity, params (Dbms Dbms, string Token)[] cells) =>
        new(name, arity, cells.ToDictionary(c => c.Dbms, c => c.Token));

    internal static readonly FunctionSpec[] All =
    {
        // ── universal: generated identically into every facade ──────────────
        Universal("Abs",   "ABS",   arity: 1),
        Universal("Sign",  "SIGN",  arity: 1),
        Universal("Floor", "FLOOR", arity: 1),
        Universal("Sqrt",  "SQRT",  arity: 1),
        Universal("Power", "POWER", arity: 2),

        // ── diverging: the entire reason this design exists ─────────────────

        // Oracle has CEIL but not CEILING; SQL Server is the mirror image.
        Diverging("Ceil", arity: 1,
            (Dbms.MySql, "CEIL"),
            (Dbms.Oracle, "CEIL"),
            (Dbms.PostgreSql, "CEIL"),
            (Dbms.Sqlite, "CEIL")),
        Diverging("Ceiling", arity: 1,
            (Dbms.MySql, "CEILING"),
            (Dbms.PostgreSql, "CEILING"),
            (Dbms.Sqlite, "CEILING"),
            (Dbms.SqlServer, "CEILING")),

        // MOD() is a function only on MySQL/Oracle/PostgreSQL; SQLite and
        // SQL Server expose modulo solely through the `%` operator, so MOD is
        // intentionally NOT generated for those namespaces.
        Diverging("Mod", arity: 2,
            (Dbms.MySql, "MOD"),
            (Dbms.Oracle, "MOD"),
            (Dbms.PostgreSql, "MOD")),

        // Single-arg ROUND: NOT valid on SQL Server (length arg is required).
        Diverging("Round", arity: 1,
            (Dbms.MySql, "ROUND"),
            (Dbms.Oracle, "ROUND"),
            (Dbms.PostgreSql, "ROUND"),
            (Dbms.Sqlite, "ROUND")),
        // Two-arg ROUND: available everywhere (semantics differ — see README §1).
        Universal("Round", "ROUND", arity: 2),

        // TRUNC: Oracle/PostgreSQL/SQLite spell it TRUNC; MySQL uses TRUNCATE
        // (and requires 2 args); SQL Server has no equivalent function.
        Diverging("Trunc", arity: 1,
            (Dbms.Oracle, "TRUNC"),
            (Dbms.PostgreSql, "TRUNC"),
            (Dbms.Sqlite, "TRUNC")),
        Diverging("Truncate", arity: 2,
            (Dbms.MySql, "TRUNCATE")),
    };
}
