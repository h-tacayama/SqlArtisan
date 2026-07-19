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
        [TargetDbms.Oracle] = "Oracle Database XE 21c (Testcontainers.Oracle module default image, gvenzl/oracle-xe:21.3.0-slim-faststart)",
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
        // WithRollup: MySQL's form, but T-SQL still accepts the legacy `GROUP BY x WITH ROLLUP`
        // (deprecated in favor of ROLLUP(...) — live-verified on SQL Server 2022).
        [new MatrixKey("WithRollup")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        // Grouping/GroupingId (Sql.G.cs XML docs, #235): single-arg GROUPING(expr) is
        // MySQL 8.0.1+/Oracle/PostgreSQL/SQL Server; the multi-column bitmask splits by
        // dialect spelling — GROUPING(a, b, ...) on MySQL/PostgreSQL, GROUPING_ID(a, ...)
        // on Oracle/SQL Server — so the two forms have disjoint dialect support.
        [new MatrixKey("Grouping", 1)] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("Grouping", 3)] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("GroupingId", 2)] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: true),

        // --- Date/time arithmetic (Sql.D.cs / Sql.A.cs XML docs) ---
        [new MatrixKey("Dateadd")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("Datediff")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("DateTrunc")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("AddMonths")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        // DateFormat: MySQL's DATE_FORMAT (Sql.D.cs XML docs, #231).
        [new MatrixKey("DateFormat")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: false, sqlServer: false),
        // Datetrunc: SQL Server 2022+'s DATETRUNC (Sql.D.cs XML docs, #231) — a distinct
        // token from DateTrunc's PostgreSQL DATE_TRUNC above, not an alternate spelling of it.
        [new MatrixKey("Datetrunc")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),

        // --- String aggregation (CHANGELOG 0.3.0-beta.1, #88) ---
        // StringAgg's 2-arg form is PostgreSQL + SQL Server + SQLite (3.44 added string_agg as
        // a group_concat alias — live-verified by the dialect sweep on the bundled 3.46); the
        // 3-arg form (inline ORDER BY) additionally works on SQLite via 3.44's
        // ORDER-BY-inside-aggregates, but not on SQL Server, which orders via the separate
        // .WithinGroup(...) chain instead (Sql.S.cs remarks; docs/expressions.md).
        [new MatrixKey("StringAgg")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: true, sqlServer: true),
        [new MatrixKey("StringAgg", 3)] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        [new MatrixKey("Listagg")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        // GroupConcat: see the key-collision caveat above — the three arity-2 overloads
        // (SQLite's positional separator, MySQL's OrderBy and Separator forms) collapse to
        // this MySQL/SQLite union, so a MySQL-only form used on SQLite stays silent.
        [new MatrixKey("GroupConcat")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: true, sqlServer: false),

        // --- PostgreSQL / Oracle single-dialect helpers ---
        [new MatrixKey("DistinctOn")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        // Dual: MySQL explicitly allows FROM DUAL too (dev.mysql.com SELECT syntax), so the
        // XML remark's "Oracle-specific" would be a false positive for MySQL users.
        [new MatrixKey("Dual")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- UPSERT / MERGE (CHANGELOG 0.3.0-beta.1, #85, #89) ---
        [new MatrixKey("OnConflict")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        [new MatrixKey("OnDuplicateKeyUpdate")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: false, sqlServer: false),
        // MERGE is PostgreSQL 15+ (version caveat — use the override if targeting an older PostgreSQL).
        [new MatrixKey("MergeInto")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("Excluded")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),

        // --- APPLY / LATERAL (CHANGELOG 0.4.0-beta.1, #122) ---
        [new MatrixKey("CrossApply")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("OuterApply")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: true),
        // Oracle 12c+ also accepts CROSS JOIN LATERAL and JOIN LATERAL ... ON (live-verified on
        // XE 21c). LeftJoinLateral stays false on Oracle: the emitted `ON TRUE` needs a boolean
        // literal, which Oracle SQL (pre-23ai) does not have.
        [new MatrixKey("CrossJoinLateral")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("LeftJoinLateral")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("JoinLateral")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),

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
        // JsonValue: MySQL added JSON_VALUE in 8.0.21 (dev.mysql.com; the mysql:8.0 baseline
        // is past that).
        // PostgreSQL's JSON_VALUE arrived in 17 — the PostgreSQL 16 baseline lacks it.
        [new MatrixKey("JsonValue")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("JsonQuery")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("JsonArrow")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        [new MatrixKey("JsonArrowText")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        // docs/expressions.md: "PostgreSQL only" for both #>/#>> operators.
        [new MatrixKey("JsonHashArrow")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("JsonHashArrowText")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        // docs/expressions.md: "PostgreSQL only" for the ARRAY[...] constructor and the
        // <@ / @> / && array predicates (#159).
        [new MatrixKey("Array")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("ArrayContainedBy")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("ArrayContains")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("ArrayOverlaps")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        // docs/expressions.md: "PostgreSQL only" for the JSONB @> / ? / ?& / ?| predicates (#159).
        [new MatrixKey("JsonbContains")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("JsonbExists")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("JsonbExistsAll")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("JsonbExistsAny")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        // docs/expressions.md: "PostgreSQL only" for the single array-typed bind behind
        // = ANY (:param), and for UNNEST (#159). The Any/All/Some keys below stay the
        // subquery-form union — the array form's PG-only verdict rides on ArrayBind.
        [new MatrixKey("ArrayBind")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("Unnest")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),

        // --- Numeric/character Oracle-only helpers (XML docs "Oracle syntax" + FunctionTests) ---
        [new MatrixKey("Lengthb")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Substrb")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Decode")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Nvl")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        // "Ceil" is emitted verbatim everywhere; SQL Server spells this function CEILING instead.
        [new MatrixKey("Ceil")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // "Ceiling" is SQL Server's/standard spelling; Oracle spells it CEIL only, but SQLite's
        // math functions provide BOTH ceil() and ceiling() (live-verified by the dialect sweep).
        [new MatrixKey("Ceiling")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: true),

        // --- Date/time Oracle-only and SQL Server-only helpers ---
        // LastDay: MySQL has LAST_DAY too — live-verified by the integration smoke catalog.
        [new MatrixKey("LastDay")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
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
        // PostgreSQL's to_number requires the format argument (smoke-catalog note); the
        // 1-arg form is Oracle-only.
        [new MatrixKey("ToNumber", 1)] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("ToTimestamp")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        // Format: SQL Server's FORMAT (Sql.F.cs XML docs, #231); both the 2-arg and
        // 3-arg (culture) overloads share this support, so one member-wide entry covers both.
        // MySQL and SQLite each have their own same-named but incompatible FORMAT() (a
        // number-decimals formatter and a printf() alias respectively) that accepts the
        // call syntax without erroring — live-verified false positive on the MySQL 8.0
        // integration sweep — so both stay false here despite the call "working".
        [new MatrixKey("Format")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        // --- REGEXP_* family: Oracle syntax; MySQL 8.0 has REGEXP_LIKE/REGEXP_REPLACE/
        // REGEXP_SUBSTR with matching signatures (live-verified by the integration smoke
        // catalog) but no REGEXP_COUNT; PostgreSQL 15+ added all four with matching
        // signatures (pgpedia.info; PostgreSQL 15 release notes), so the PostgreSQL 16
        // baseline has them — the earlier Oracle-only classification of RegexpLike was a
        // false positive for both MySQL and PostgreSQL users and is corrected here.
        [new MatrixKey("RegexpLike")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("RegexpCount")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("RegexpReplace")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("RegexpSubstr")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),

        // --- Window functions ---
        // NthValue: XML docs + docs/functions.md + docs/expressions.md all state "Not supported by SQL Server".
        [new MatrixKey("NthValue")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),

        // --- Set operators ---
        // Minus: Oracle's spelling of EXCEPT.
        [new MatrixKey("Minus")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- Sequences ---
        // Nextval/Currval are a name collision (see the caveat above): Sql.Nextval("seq") is
        // PostgreSQL's function form, while Sequence("seq").Nextval is Oracle's — a property on
        // DbSequence with the same member name. A PostgreSQL-only entry would false-positive on
        // the correct Oracle form, so both are the union of the two forms' dialects.
        [new MatrixKey("Nextval")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("Currval")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("NextValueFor")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("Sequence")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- RETURNING (docs/query-statements.md: "supported by Oracle, PostgreSQL, and SQLite
        // (3.35+). Not supported by SQL Server (uses OUTPUT) or MySQL.") ---
        [new MatrixKey("Returning")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // "Into" (the RETURNING ... INTO chain) is Oracle-specific — narrower than Returning itself.
        [new MatrixKey("Into")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- OUTPUT (SQL Server): the RETURNING counterpart, plus the INSERTED./DELETED.
        // pseudo-table references. All SQL-Server-only. The OUTPUT ... INTO redirect is
        // Into arity 2 (DbTableBase, params DbColumn[]); the arity-priority lookup keeps it
        // distinct from the arity-1 Oracle "Into" above (same split as JOIN/MERGE "Using").
        [new MatrixKey("Output")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("Inserted")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("Deleted")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("Into", 2)] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),

        // --- MERGE sub-states narrower than the surrounding MergeInto/Using/On/WhenMatched/
        // WhenNotMatched scope (Oracle/PostgreSQL 15+/SQL Server) — a coverage gap the initial
        // pass left silent since these method names had no entry of their own at all.
        [new MatrixKey("WhenNotMatchedBySource")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        // ThenDelete: PostgreSQL 15+ MERGE also supports WHEN MATCHED THEN DELETE (Oracle
        // does not — its delete is the in-clause DeleteWhere).
        [new MatrixKey("ThenDelete")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("DeleteWhere")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // ============ Full-coverage expansion (#93 step 1) ============
        // Everything below completes coverage of the public surface. Universal rows assert
        // "valid on all five baselines" — most are ANSI core with no dialect note anywhere in
        // the repo; the non-obvious cells cite their source. Restricted rows below were
        // individually verified against official vendor documentation (cited per group);
        // the live-engine sweep (#93 step 2-3) is the final arbiter for all of them.

        // --- Statement / clause / builder core (universal) ---
        [new MatrixKey("Select")] = DbmsSupport.All,
        [new MatrixKey("InsertInto")] = DbmsSupport.All,
        // InsertIgnoreInto: INSERT IGNORE is MySQL-only; PostgreSQL/SQLite spell the
        // do-nothing UPSERT as ON CONFLICT DO NOTHING, Oracle/SQL Server as MERGE.
        [new MatrixKey("InsertIgnoreInto")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Update")] = DbmsSupport.All,
        [new MatrixKey("DeleteFrom")] = DbmsSupport.All,
        [new MatrixKey("From")] = DbmsSupport.All,
        [new MatrixKey("Where")] = DbmsSupport.All,
        [new MatrixKey("GroupBy")] = DbmsSupport.All,
        [new MatrixKey("Having")] = DbmsSupport.All,
        [new MatrixKey("OrderBy")] = DbmsSupport.All,
        // On: join ON (universal) + MERGE ON (Oracle/PostgreSQL/SQL Server) share the name — union.
        [new MatrixKey("On")] = DbmsSupport.All,
        // Set: UPDATE SET (universal); the SET-like INSERT emits standard INSERT (docs note), not MySQL's INSERT ... SET.
        [new MatrixKey("Set")] = DbmsSupport.All,
        // Values: single-row INSERT is universal; Oracle rejects multi-row VALUES (#87) but the
        // row count is a call-site value the matrix key cannot see — union, under-restricts Oracle.
        [new MatrixKey("Values")] = DbmsSupport.All,
        [new MatrixKey("InnerJoin")] = DbmsSupport.All,
        [new MatrixKey("LeftJoin")] = DbmsSupport.All,
        // RightJoin: SQLite added RIGHT JOIN in 3.39 (bundled baseline 3.46+).
        [new MatrixKey("RightJoin")] = DbmsSupport.All,
        [new MatrixKey("CrossJoin")] = DbmsSupport.All,
        // FullJoin: MySQL has no FULL [OUTER] JOIN at all; SQLite added it in 3.39 (baseline OK).
        [new MatrixKey("FullJoin")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: true, sqlServer: true),
        // NATURAL JOIN family (#197): standard SQL, but SQL Server has no NATURAL JOIN spelling at all.
        [new MatrixKey("NaturalJoin")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        [new MatrixKey("NaturalLeftJoin")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // NaturalRightJoin: SQLite added RIGHT JOIN in 3.39 (bundled baseline 3.46+), NATURAL included.
        [new MatrixKey("NaturalRightJoin")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // NaturalFullJoin: MySQL has no FULL JOIN at all (see FullJoin above), so NATURAL FULL is out too.
        [new MatrixKey("NaturalFullJoin")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // JOIN ... USING (#197): arity 2 (DbColumn, params DbColumn[]) avoids colliding with MERGE's
        // arity-1 Using(TableReference) below — see the MatrixKey collision caveat above.
        [new MatrixKey("Using", 2)] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        [new MatrixKey("With")] = DbmsSupport.All,
        // WithRecursive: the RECURSIVE keyword itself is the gap — Oracle and SQL Server write
        // recursive CTEs as plain WITH and reject WITH RECURSIVE; MySQL 8.0+/PostgreSQL/SQLite
        // require it for recursion.
        [new MatrixKey("WithRecursive")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        // Asterisk: Sql.Asterisk (SELECT *) and TableReference.Asterisk (t.*) share the name — both universal.
        [new MatrixKey("Asterisk")] = DbmsSupport.All,
        [new MatrixKey("Distinct")] = DbmsSupport.All,
        // Hints: the mechanism (verbatim text after SELECT) is universal; the hint text itself
        // is the caller's per-dialect responsibility.
        [new MatrixKey("Hints")] = DbmsSupport.All,
        // TOP (n) and its PERCENT / WITH TIES modifiers — SQL Server select prefix.
        [new MatrixKey("Top")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("Percent")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("WithTies")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: false, sqlite: false, sqlServer: true),
        [new MatrixKey("Group")] = DbmsSupport.All,
        [new MatrixKey("Null")] = DbmsSupport.All,
        [new MatrixKey("Bind")] = DbmsSupport.All,
        [new MatrixKey("ConditionIf")] = DbmsSupport.All,
        [new MatrixKey("As")] = DbmsSupport.All,
        [new MatrixKey("Asc")] = DbmsSupport.All,
        [new MatrixKey("Desc")] = DbmsSupport.All,

        // --- Expressions / conditions (universal, ANSI core) ---
        [new MatrixKey("Case")] = DbmsSupport.All,
        [new MatrixKey("When")] = DbmsSupport.All,
        [new MatrixKey("Then")] = DbmsSupport.All,
        [new MatrixKey("Else")] = DbmsSupport.All,
        [new MatrixKey("Cast")] = DbmsSupport.All,
        [new MatrixKey("Exists")] = DbmsSupport.All,
        [new MatrixKey("NotExists")] = DbmsSupport.All,
        [new MatrixKey("Not")] = DbmsSupport.All,
        [new MatrixKey("In")] = DbmsSupport.All,
        [new MatrixKey("NotIn")] = DbmsSupport.All,
        [new MatrixKey("Between")] = DbmsSupport.All,
        [new MatrixKey("NotBetween")] = DbmsSupport.All,
        [new MatrixKey("Like")] = DbmsSupport.All,
        [new MatrixKey("NotLike")] = DbmsSupport.All,
        // Escape: docs/expressions.md — "supported identically across all dialects".
        [new MatrixKey("Escape")] = DbmsSupport.All,
        [new MatrixKey("IsNull")] = DbmsSupport.All,
        [new MatrixKey("IsNotNull")] = DbmsSupport.All,
        // All/Any/Some: the CHANGELOG's "supported on all five dialects" (#196) is wrong for
        // SQLite — its expression grammar has no quantified comparisons at all, live-verified
        // by the dialect sweep ('near "ALL": syntax error'). docs corrected to match.
        // The array-operand overloads (#159) collide here at arity 1 (MatrixKey is type-blind),
        // so these entries stay the subquery-form support; the PG-only array form is guarded
        // through its ArrayBind argument. Known false negative: an array-typed column or
        // ARRAY[...] operand with no ArrayBind call goes unflagged off-PG.
        [new MatrixKey("All")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("Any")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("Some")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("Coalesce")] = DbmsSupport.All,
        [new MatrixKey("Nullif")] = DbmsSupport.All,

        // --- Functions with no dialect variance across the baselines ---
        [new MatrixKey("Abs")] = DbmsSupport.All,
        // Floor/Power/Sqrt/Sign on SQLite are math functions (3.35+, SQLITE_ENABLE_MATH_FUNCTIONS;
        // enabled in the bundled e_sqlite3 build) — sweep-confirm along with Mod.
        [new MatrixKey("Floor")] = DbmsSupport.All,
        [new MatrixKey("Power")] = DbmsSupport.All,
        [new MatrixKey("Sqrt")] = DbmsSupport.All,
        [new MatrixKey("Sign")] = DbmsSupport.All,
        [new MatrixKey("Lower")] = DbmsSupport.All,
        [new MatrixKey("Upper")] = DbmsSupport.All,
        [new MatrixKey("Replace")] = DbmsSupport.All,
        [new MatrixKey("Avg")] = DbmsSupport.All,
        [new MatrixKey("Count")] = DbmsSupport.All,
        [new MatrixKey("Max")] = DbmsSupport.All,
        [new MatrixKey("Min")] = DbmsSupport.All,
        [new MatrixKey("Sum")] = DbmsSupport.All,
        [new MatrixKey("CurrentTimestamp")] = DbmsSupport.All,
        // Concat split by declared arity (#234): Oracle's native CONCAT takes exactly 2
        // arguments, so the 2-arg form is universal but the 3+-arg form is invalid there.
        // SQLite: concat() since 3.44 (baseline 3.46+); SQL Server: CONCAT since 2012.
        [new MatrixKey("Concat", 2)] = DbmsSupport.All,
        [new MatrixKey("Concat", 4)] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: true),
        // DoublePipe (Sql.D.cs XML docs, #234): native on Oracle/PostgreSQL/SQLite (every
        // version). MySQL rejects it under the default sql_mode's PIPES_AS_CONCAT-off
        // meaning — || is logical OR there, valid SQL with silently different semantics,
        // exactly the trap this entry exists to flag. SQL Server has no || operator at all
        // (its concatenation operator is +, the existing AdditionOperator).
        [new MatrixKey("DoublePipe")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),

        // --- Window / analytic (universal on the baselines: MySQL 8.0+, SQLite 3.25+, SQL Server 2012+) ---
        [new MatrixKey("Rank")] = DbmsSupport.All,
        [new MatrixKey("RowNumber")] = DbmsSupport.All,
        [new MatrixKey("DenseRank")] = DbmsSupport.All,
        [new MatrixKey("CumeDist")] = DbmsSupport.All,
        [new MatrixKey("PercentRank")] = DbmsSupport.All,
        [new MatrixKey("Ntile")] = DbmsSupport.All,
        [new MatrixKey("Lag")] = DbmsSupport.All,
        [new MatrixKey("Lead")] = DbmsSupport.All,
        [new MatrixKey("FirstValue")] = DbmsSupport.All,
        [new MatrixKey("LastValue")] = DbmsSupport.All,
        [new MatrixKey("Over")] = DbmsSupport.All,
        [new MatrixKey("PartitionBy")] = DbmsSupport.All,
        [new MatrixKey("Rows")] = DbmsSupport.All,
        // Range: T-SQL only allows RANGE with UNBOUNDED/CURRENT ROW bounds (no numeric offsets) —
        // a bound-value nuance the key cannot see; union, under-restricts SQL Server.
        [new MatrixKey("Range")] = DbmsSupport.All,
        [new MatrixKey("RowsBetween")] = DbmsSupport.All,
        [new MatrixKey("RangeBetween")] = DbmsSupport.All,
        [new MatrixKey("CurrentRow")] = DbmsSupport.All,
        [new MatrixKey("Preceding")] = DbmsSupport.All,
        [new MatrixKey("Following")] = DbmsSupport.All,
        [new MatrixKey("UnboundedPreceding")] = DbmsSupport.All,
        [new MatrixKey("UnboundedFollowing")] = DbmsSupport.All,

        // --- Character/numeric functions with real dialect gaps (vendor docs, per row) ---
        // Mod: T-SQL has no MOD() function (only the % operator).
        [new MatrixKey("Mod")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // Length: T-SQL spells this LEN(); no LENGTH().
        [new MatrixKey("Length")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // Substr: T-SQL spells this SUBSTRING(); no SUBSTR.
        [new MatrixKey("Substr")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // Lpad/Rpad: no native LPAD/RPAD on SQLite or SQL Server (even 2022). MySQL's
        // LPAD/RPAD require the pad argument (smoke-catalog note), so the 2-arg
        // pad-with-spaces form is Oracle/PostgreSQL only.
        [new MatrixKey("Lpad")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("Lpad", 2)] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("Rpad")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("Rpad", 2)] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        // Ltrim/Rtrim: 1-arg universal; the 2-arg trim-characters form does not exist on MySQL
        // (LTRIM(str) only), and on SQL Server requires 2022 with compatibility level 160
        // (learn.microsoft.com LTRIM/RTRIM pages) — the baseline's default for new databases.
        [new MatrixKey("Ltrim")] = DbmsSupport.All,
        [new MatrixKey("Ltrim", 2)] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: true, sqlServer: true),
        [new MatrixKey("Rtrim")] = DbmsSupport.All,
        [new MatrixKey("Rtrim", 2)] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: true, sqlServer: true),
        // Trim: 1-arg universal (T-SQL TRIM since 2017). The 2-arg overload emits the ANSI
        // keyword form TRIM(BOTH ch FROM src) (TrimFunction.cs): SQLite only accepts its own
        // positional trim(X, Y), not the FROM form; SQL Server accepts BOTH ... FROM on
        // 2022/compat 160.
        [new MatrixKey("Trim")] = DbmsSupport.All,
        [new MatrixKey("Trim", 2)] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        // Round: T-SQL ROUND requires 2-3 arguments — the 1-arg form is invalid on SQL Server.
        [new MatrixKey("Round")] = DbmsSupport.All,
        [new MatrixKey("Round", 1)] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        // Instr: MySQL and SQLite both have the native 2-arg INSTR(str, substr) — an arity
        // split. The 3/4-arg forms are Oracle-only (member-level row); PostgreSQL and
        // SQL Server have no INSTR at any arity.
        [new MatrixKey("Instr")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Instr", 2)] = new DbmsSupport(mySql: true, oracle: true, postgreSql: false, sqlite: true, sqlServer: false),
        // Greatest/Least: no SQLite equivalent (it uses multi-argument scalar MAX/MIN instead);
        // SQL Server added GREATEST/LEAST in 2022 (the baseline).
        [new MatrixKey("Greatest")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("Least")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),

        // --- Overloaded C# operators (#219): keyed by the CLR operator method name verbatim
        // (op_Modulus -> sqlartisan_construct_op_modulus). All are binary; apart from % they
        // emit SQL-92 core grammar (comparisons, AND/OR, + - * /) — universal on every baseline.
        [new MatrixKey("op_Addition")] = DbmsSupport.All,
        [new MatrixKey("op_Subtraction")] = DbmsSupport.All,
        [new MatrixKey("op_Multiply")] = DbmsSupport.All,
        [new MatrixKey("op_Division")] = DbmsSupport.All,
        // op_Modulus: Oracle has no % arithmetic operator — its spelling is MOD(n, m), the
        // exact mirror of the Mod entry's sqlServer: false above.
        [new MatrixKey("op_Modulus")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: true),
        [new MatrixKey("op_Equality")] = DbmsSupport.All,
        [new MatrixKey("op_Inequality")] = DbmsSupport.All,
        [new MatrixKey("op_LessThan")] = DbmsSupport.All,
        [new MatrixKey("op_GreaterThan")] = DbmsSupport.All,
        [new MatrixKey("op_LessThanOrEqual")] = DbmsSupport.All,
        [new MatrixKey("op_GreaterThanOrEqual")] = DbmsSupport.All,
        [new MatrixKey("op_BitwiseAnd")] = DbmsSupport.All,
        [new MatrixKey("op_BitwiseOr")] = DbmsSupport.All,

        // --- Date/time keywords with gaps ---
        // CurrentDate/CurrentTime: T-SQL supports neither ANSI keyword (only CURRENT_TIMESTAMP);
        // Oracle has CURRENT_DATE and CURRENT_TIMESTAMP but no CURRENT_TIME (no TIME type).
        [new MatrixKey("CurrentDate")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        [new MatrixKey("CurrentTime")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        // Extract: ANSI EXTRACT(part FROM source) — no SQLite function, no T-SQL support (DATEPART).
        [new MatrixKey("Extract")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),

        // --- Aggregate chains ---
        // Filter: docs/expressions.md — "Native on PostgreSQL and SQLite".
        [new MatrixKey("Filter")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        // WithinGroup: union of its three hosts — Listagg (Oracle), StringAgg (SQL Server),
        // PercentileCont/Disc (Oracle + PostgreSQL).
        [new MatrixKey("WithinGroup")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        // PercentileCont/Disc: Sql.P.cs remarks — Oracle both forms, PostgreSQL WithinGroup-only,
        // SQL Server Over-only; MySQL/SQLite not at all. The per-chain narrowing is a
        // builder-state nuance the key cannot see — member-level union of the three.
        [new MatrixKey("PercentileCont")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("PercentileDisc")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),

        // --- Set operators (ISetOperator properties) ---
        [new MatrixKey("Union")] = DbmsSupport.All,
        [new MatrixKey("UnionAll")] = DbmsSupport.All,
        // Except/Intersect: MySQL added both in 8.0.31 (the floating mysql:8.0 baseline is past
        // that); Oracle added EXCEPT in 21c (baseline is 23ai Free) — oracle-base.com 21c article.
        [new MatrixKey("Except")] = DbmsSupport.All,
        [new MatrixKey("Intersect")] = DbmsSupport.All,
        // The ALL variants: MySQL 8.0.31+, Oracle 21c+, PostgreSQL always; SQLite and SQL Server
        // support ALL only on UNION.
        [new MatrixKey("ExceptAll")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("IntersectAll")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        // MinusAll: Oracle 21c+ spelling of EXCEPT ALL.
        [new MatrixKey("MinusAll")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- Pagination (IPagination / ILimitOffsetBuilder / IOffsetFetchBuilder XML remarks) ---
        [new MatrixKey("Limit")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        // Offset: standalone OFFSET is PostgreSQL-only, after Limit(...) it is MySQL/PostgreSQL/
        // SQLite — same member name on two interfaces, so this is their union (a standalone
        // Offset on MySQL/SQLite stays silent rather than warn).
        [new MatrixKey("Offset")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        // OffsetRows/FetchNext: the ANSI OFFSET ... FETCH pair — Oracle 12c+, PostgreSQL, and
        // SQL Server 2012+ (PostgreSQL live-verified by the dialect sweep).
        [new MatrixKey("OffsetRows")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("FetchNext")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        // FetchFirst: standalone form — Oracle 12c+ and PostgreSQL (IPagination remark); SQL
        // Server needs the OffsetRows(...) prefix instead.
        [new MatrixKey("FetchFirst")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),

        // --- FOR UPDATE (row locking): SQLite has no row locking; SQL Server uses lock hints,
        // not ANSI FOR UPDATE. NOWAIT/SKIP LOCKED/OF: MySQL 8.0+, Oracle, PostgreSQL.
        // WAIT n is Oracle-only.
        [new MatrixKey("ForUpdate")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        // Of: SqlArtisan's Of(DbColumn) emits Oracle's FOR UPDATE OF <column> form;
        // MySQL's and PostgreSQL's FOR UPDATE OF take a table name, not a column, so the
        // emitted form is Oracle-only (live-verified: the statement catalog runs
        // ForUpdateOf on Oracle alone).
        [new MatrixKey("Of")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),
        [new MatrixKey("Nowait")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("SkipLocked")] = new DbmsSupport(mySql: true, oracle: true, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("Wait")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: false, sqlite: false, sqlServer: false),

        // --- ORDER BY null ordering: no NULLS FIRST/LAST on MySQL or SQL Server; SQLite 3.30+.
        [new MatrixKey("NullsFirst")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),
        [new MatrixKey("NullsLast")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: true, sqlServer: false),

        // --- PostgreSQL full-text helpers (Sql.T.cs / Sql.P.cs "PostgreSQL syntax") ---
        [new MatrixKey("ToTsvector")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("ToTsquery")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),
        [new MatrixKey("PlaintoTsquery")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: false),

        // --- MERGE / UPSERT chain steps (same scope as their statements) ---
        // MergeBuilder's arity-1 Using(TableReference); see the JOIN's arity-2 entry above (#197).
        // The subquery source rides this key (a TableReference subtype through the same
        // Using overload); only the VALUES source below narrows the scope (Oracle excluded).
        [new MatrixKey("Using")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        // MERGE literal-row source (VALUES (…),(…)); Oracle has no VALUES row constructor in USING.
        [new MatrixKey("ValuesTable")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("WhenMatched")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("WhenNotMatched")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("ThenInsert")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("ThenUpdateSet")] = new DbmsSupport(mySql: false, oracle: true, postgreSql: true, sqlite: false, sqlServer: true),
        [new MatrixKey("DoNothing")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),
        [new MatrixKey("DoUpdateSet")] = new DbmsSupport(mySql: false, oracle: false, postgreSql: true, sqlite: true, sqlServer: false),

        // --- GROUP_CONCAT's SEPARATOR clause factory (MySQL keyword form; SQLite uses the
        // positional 2-arg GroupConcat overload instead).
        [new MatrixKey("Separator")] = new DbmsSupport(mySql: true, oracle: false, postgreSql: false, sqlite: false, sqlServer: false),

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
