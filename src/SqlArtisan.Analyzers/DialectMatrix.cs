using System.Collections.Generic;
using System.Linq;

namespace SqlArtisan.Analyzers;

/// <summary>
/// The verified dialect-support matrix for SqlArtisan's public API (#93). Each
/// entry asserts, with primary-source evidence (CHANGELOG, XML docs, or the
/// integration test matrix), which of the five dialects a construct is valid
/// on. Absence of an entry is silence, not a claim of universal support — the
/// analyzer only ever warns about what this matrix actually asserts (ADR 0003's
/// "degradable" design: an incomplete matrix cannot cause a false positive).
///
/// <para>
/// This is a <b>partial</b> matrix: it currently covers the constructs with
/// known, confidently-sourced dialect restrictions. Reaching full coverage of
/// every public member (the 1.0 completion condition, enforced by a coverage
/// gate in the test project once complete) is tracked as follow-up work — see
/// the sa-add-sql-function skill's fifth touch point.
/// </para>
///
/// <para>
/// <b>Key collision caveat:</b> a <see cref="MatrixKey"/> is (member name,
/// optional arity) with no regard to declaring type or parameter types, so two
/// unrelated members that happen to share a name and declared parameter count
/// would collide into one entry. <c>Match</c> below is a real example: MySQL's
/// <c>Match(object, params object[])</c> and SQLite's
/// <c>Match(DbTableBase, object)</c> both declare exactly two parameters, so
/// they cannot be told apart by arity — the entry below is their support
/// *union*, which only under-restricts (a false negative, never a false
/// positive). Check for this before adding a new entry with the same name as
/// an existing one.
/// </para>
/// </summary>
internal static class DialectMatrix
{
    /// <summary>
    /// The engine version each dialect's entries were verified against — the
    /// integration test matrix's fixtures where a live engine is used, or the
    /// driver's bundled version for SQLite (in-process, no container). A
    /// construct verified as unsupported on an older version may work on a
    /// newer one; use the <c>sqlartisan_construct_*</c> override to correct
    /// for that rather than treating this table as exhaustive across versions.
    /// </summary>
    public static readonly IReadOnlyDictionary<TargetDbms, string> VerifiedAgainstVersion = new Dictionary<TargetDbms, string>
    {
        [TargetDbms.MySql] = "MySQL 8.0 (Testcontainers `mysql:8.0`)",
        [TargetDbms.Oracle] = "Oracle Database Free (Testcontainers.Oracle module default image, gvenzl/oracle-free)",
        [TargetDbms.PostgreSql] = "PostgreSQL 16 (Testcontainers `postgres:16-alpine`)",
        [TargetDbms.Sqlite] = "the SQLite version bundled with Microsoft.Data.Sqlite 9.0.5 (in-process, no container)",
        [TargetDbms.SqlServer] = "SQL Server 2022 (Testcontainers `mcr.microsoft.com/mssql/server:2022-latest`)",
    };

    private static readonly Dictionary<MatrixKey, DbmsSupport> Entries = new()
    {
        // --- GROUP BY extensions (#93 issue thread; PR #130) ---
        [new MatrixKey("Rollup")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("Cube")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("GroupingSets")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("WithRollup")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: false, sqlServer: false),

        // --- Date/time arithmetic (Sql.D.cs / Sql.A.cs XML docs) ---
        [new MatrixKey("Dateadd")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("Datediff")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("DateTrunc")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("AddMonths")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- String aggregation (CHANGELOG 0.3.0-beta.1, #88) ---
        // StringAgg's 2-arg form is PostgreSQL + SQL Server; the 3-arg form (inline ORDER BY) is
        // PostgreSQL-only — SQL Server orders via the separate .WithinGroup(...) chain instead
        // (Sql.S.cs remarks; docs/expressions.md). Arity-split to avoid under-restricting SQL Server.
        [new MatrixKey("StringAgg")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("StringAgg", 3)] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("Listagg")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("GroupConcat")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: true, sqlServer: false),

        // --- PostgreSQL / Oracle single-dialect helpers ---
        [new MatrixKey("DistinctOn")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("Dual")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- UPSERT / MERGE (CHANGELOG 0.3.0-beta.1, #85, #89) ---
        [new MatrixKey("OnConflict")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        [new MatrixKey("OnDuplicateKeyUpdate")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: false, sqlServer: false),
        // MERGE is PostgreSQL 15+ (version caveat — use the override if targeting an older PostgreSQL).
        [new MatrixKey("MergeInto")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("Excluded")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),

        // --- APPLY / LATERAL (CHANGELOG 0.4.0-beta.1, #122) ---
        [new MatrixKey("CrossApply")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("OuterApply")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("CrossJoinLateral")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("LeftJoinLateral")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("JoinLateral")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),

        // --- Full-text search (CHANGELOG 0.5.0-beta.2, #153) ---
        // Match: see the key-collision caveat above — union of MySQL's and SQLite's overloads.
        [new MatrixKey("Match")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: true, sqlServer: false),
        [new MatrixKey("Against")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("AgainstScore")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("ContainsScore")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Score")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("TsMatch")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("Contains")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("Freetext")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),

        // --- JSON (CHANGELOG 0.5.0-beta.2, #152) ---
        [new MatrixKey("JsonExtract")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: true, sqlServer: false),
        [new MatrixKey("JsonValue")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("JsonQuery")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("JsonArrow")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        [new MatrixKey("JsonArrowText")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        // docs/expressions.md: "PostgreSQL only" for both #>/#>> operators.
        [new MatrixKey("JsonHashArrow")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("JsonHashArrowText")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),

        // --- Numeric/character Oracle-only helpers (XML docs "Oracle syntax" + FunctionTests) ---
        [new MatrixKey("Lengthb")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Substrb")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Decode")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Nvl")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("RegexpLike")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        // "Ceil" is emitted verbatim everywhere; SQL Server spells this function CEILING instead.
        [new MatrixKey("Ceil")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // "Ceiling" is SQL Server's/standard spelling; Oracle and SQLite spell it CEIL instead.
        [new MatrixKey("Ceiling")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: false, sqlServer: true),

        // --- Date/time Oracle-only and SQL Server-only helpers ---
        [new MatrixKey("LastDay")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("MonthsBetween")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Sysdate")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Systimestamp")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Datepart")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),

        // --- Conversion functions: confirmed documentation gap (XML docs say "Oracle syntax" only,
        // but .claude/rules/unit-tests.md whitelists TO_CHAR as PG-valid and every test targets the
        // default (PostgreSQL) build for all four — the matrix reflects verified behavior, not the
        // stale remark). See docs/analyzer.md follow-up: the XML docs/docs/functions.md should be
        // corrected to match.
        [new MatrixKey("ToChar")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("ToDate")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("ToNumber")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("ToTimestamp")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        // RegexpCount/RegexpReplace/RegexpSubstr show the identical "Oracle syntax" remark plus a
        // default-.Build() test pattern hinting at the same PostgreSQL gap as ToChar, but with no
        // rule-file confirmation backing it (medium, not high, confidence) — deliberately left
        // unentered pending an integration-test check rather than guessed either way.

        // --- Window functions ---
        // NthValue: XML docs + docs/functions.md + docs/expressions.md all state "Not supported by SQL Server".
        [new MatrixKey("NthValue")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),

        // --- Set operators ---
        // Minus: Oracle's spelling of EXCEPT.
        [new MatrixKey("Minus")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- Sequences ---
        [new MatrixKey("Nextval")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("Currval")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("NextValueFor")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("Sequence")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- RETURNING (docs/query-statements.md: "supported by Oracle, PostgreSQL, and SQLite
        // (3.35+). Not supported by SQL Server (uses OUTPUT) or MySQL.") ---
        [new MatrixKey("Returning")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // "Into" (the RETURNING ... INTO chain) is Oracle-specific — narrower than Returning itself.
        [new MatrixKey("Into")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- MERGE sub-states narrower than the surrounding MergeInto/Using/On/WhenMatched/
        // WhenNotMatched scope (Oracle/PostgreSQL 15+/SQL Server) — a coverage gap the initial
        // pass left silent since these method names had no entry of their own at all.
        [new MatrixKey("WhenNotMatchedBySource")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("ThenDelete")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("DeleteWhere")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- Trunc is deliberately NOT entered: Sql.Trunc(expr[, format]) carries one dialect
        // support set for a numeric argument (Oracle+PostgreSQL) and a DIFFERENT, disjoint one for
        // a date/time argument (Oracle only) — same two overloads, distinguished only by the
        // call site's runtime value, not by arity or declared parameter type. The current
        // MatrixKey (member name + declared arity) cannot express this; asserting either dialect
        // set as "the" entry for "Trunc" would misclassify the other half. Needs an
        // argument-type-aware key shape before it can be entered safely — tracked as follow-up,
        // not guessed around.
    };

    public static bool TryGetEntry(string memberName, int? arity, out DbmsSupport support, out bool wasArityMatch) =>
        TryGetEntryFrom(Entries, memberName, arity, out support, out wasArityMatch);

    /// <summary>
    /// The arity-priority lookup, factored out so tests can exercise it against
    /// a synthetic dictionary rather than polluting <see cref="Entries"/> (which
    /// asserts only primary-source-verified real dialect facts) with fake rows.
    /// </summary>
    internal static bool TryGetEntryFrom(
        IReadOnlyDictionary<MatrixKey, DbmsSupport> entries,
        string memberName,
        int? arity,
        out DbmsSupport support,
        out bool wasArityMatch)
    {
        if (arity.HasValue && entries.TryGetValue(new MatrixKey(memberName, arity), out support))
        {
            wasArityMatch = true;
            return true;
        }

        wasArityMatch = false;
        return entries.TryGetValue(new MatrixKey(memberName), out support);
    }

    public static IEnumerable<string> AllOverrideKeys => Entries.Keys.Select(ToOverrideKey);

    /// <summary>Exposed for the integrity test (matrix keys resolve to real public members).</summary>
    internal static IEnumerable<MatrixKey> AllKeys => Entries.Keys;

    private static string ToOverrideKey(MatrixKey key) => key.Arity is { } arity
        ? ConstructKeyNaming.ArityKey(key.MemberName, arity)
        : ConstructKeyNaming.MemberKey(key.MemberName);
}
