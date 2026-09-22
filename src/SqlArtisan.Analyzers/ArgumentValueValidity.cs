using System;
using System.Collections.Generic;

namespace SqlArtisan.Analyzers;

/// <summary>
/// The per-(value, dialect) facts SQLA0104 reads: which <c>RegexpOptions</c>
/// members each engine's match-parameter alphabet has (#528), and which
/// row-count constructs reject a negative constant on which engine (#529).
/// </summary>
/// <remarks>
/// Both tables are live-verified per cell and twinned in the per-engine
/// integration tests; a pair absent from either is a fact this rule does not
/// have, so it stays silent rather than guessing.
/// </remarks>
internal static class ArgumentValueValidity
{
    // --- RegexpOptions: the match-parameter alphabet, per dialect (#528) ---

    // The parameter SQLA0104 reads the literal RegexpOptions out of — each entry
    // matches that factory's own parameter name in Sql.R.cs. RegexpCount is listed
    // for Oracle and PostgreSQL; the matrix owns MySQL, which has no REGEXP_COUNT.
    internal static readonly Dictionary<string, string> MatchOptionParameterName = new(
        StringComparer.Ordinal)
    {
        ["RegexpCount"] = "options",
        ["RegexpInstr"] = "options",
        ["RegexpLike"] = "options",
        ["RegexpReplace"] = "options",
        ["RegexpSubstr"] = "options",
    };

    // dev.mysql.com Regular Expressions, match_type: c, i, m, n and u. MySQL 8.0
    // rejects 'x' with `Incorrect arguments to regexp_like`, and its own 'u' has
    // no RegexpOptions member, so ExcludingWhiteSpace is the one gap here.
    private static readonly HashSet<string> MySqlMatchOptions = new(StringComparer.Ordinal)
    {
        "CaseSensitive", "CaseInsensitive", "MultipleLines", "NewLine",
    };

    // docs.oracle.com REGEXP_LIKE, match_param: every letter RegexpOptions can
    // emit is valid on Oracle XE 21.3.0.
    private static readonly HashSet<string> OracleMatchOptions = new(StringComparer.Ordinal)
    {
        "CaseSensitive", "CaseInsensitive", "MultipleLines", "NewLine", "ExcludingWhiteSpace",
    };

    // postgresql.org POSIX Regular Expressions, embedded-option letters: a
    // superset of what RegexpOptions can emit, all valid on PostgreSQL 16.13.
    private static readonly HashSet<string> PostgreSqlMatchOptions = new(StringComparer.Ordinal)
    {
        "CaseSensitive", "CaseInsensitive", "MultipleLines", "NewLine", "ExcludingWhiteSpace",
    };

    private static readonly Dictionary<TargetDbms, HashSet<string>> MatchOptionTable = new()
    {
        [TargetDbms.MySql] = MySqlMatchOptions,
        [TargetDbms.Oracle] = OracleMatchOptions,
        [TargetDbms.PostgreSql] = PostgreSqlMatchOptions,
    };

    // --- Row counts: where a negative constant is rejected (#529) ---

    // The parameter SQLA0104 reads the row count out of. Offset/OffsetRows are
    // absent by decision, not for want of evidence (#532): an offset is the
    // argument the paging recipe passes as a variable, which this rule cannot see.
    internal static readonly Dictionary<string, string> RowCountParameterName = new(
        StringComparer.Ordinal)
    {
        ["FetchFirst"] = "count",
        ["FetchNext"] = "count",
        ["Limit"] = "count",
        ["Top"] = "count",
    };

    // Absence is silence, and two absences are load-bearing: Oracle XE 21.3.0
    // executes `FETCH FIRST -1 ROWS ONLY` and SQLite 3.50.4 reads `LIMIT -1` as
    // "no limit", so reporting either would flag code that runs.
    private static readonly HashSet<(string Member, TargetDbms Dbms)> NegativeRowCountRejected =
        new()
        {
            // PostgreSQL 16.13 raises `LIMIT must not be negative` for all three
            // spellings, the two FETCH ones included.
            ("FetchFirst", TargetDbms.PostgreSql),
            ("FetchNext", TargetDbms.PostgreSql),
            ("Limit", TargetDbms.PostgreSql),

            // MySQL 8.0's LIMIT grammar takes an unsigned integer only, so a
            // negative count is a parse error.
            ("Limit", TargetDbms.MySql),

            // `A TOP N or FETCH rowcount value may not be negative.` (SQL Server 2022).
            ("FetchNext", TargetDbms.SqlServer),
            ("Top", TargetDbms.SqlServer),
        };

    /// <summary>
    /// The valid <c>RegexpOptions</c> member-name set for <paramref name="dbms"/>,
    /// or <see langword="null"/> when this rule has no alphabet for that engine —
    /// nothing to check, stay silent.
    /// </summary>
    public static HashSet<string>? MatchOptionsFor(TargetDbms dbms) =>
        MatchOptionTable.TryGetValue(dbms, out HashSet<string>? set) ? set : null;

    /// <summary>
    /// Whether <paramref name="dbms"/> is measured to reject a negative constant
    /// count for <paramref name="memberName"/>.
    /// </summary>
    public static bool RejectsNegativeRowCount(string memberName, TargetDbms dbms) =>
        NegativeRowCountRejected.Contains((memberName, dbms));

    /// <summary>Every <c>RegexpOptions</c> name appearing in at least one
    /// alphabet — the parity gate's coverage check.</summary>
    internal static IEnumerable<string> AllKnownMatchOptionNames
    {
        get
        {
            HashSet<string> seen = new(StringComparer.Ordinal);

            foreach (HashSet<string> set in MatchOptionTable.Values)
            {
                seen.UnionWith(set);
            }

            return seen;
        }
    }

    /// <summary>Every (member, dialect) cell the row-count table rejects on —
    /// the parity gate's coverage check.</summary>
    internal static IEnumerable<(string Member, TargetDbms Dbms)> AllRejectedRowCountCells =>
        NegativeRowCountRejected;
}
