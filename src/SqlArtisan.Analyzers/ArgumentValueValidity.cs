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
    // for Oracle, PostgreSQL and SQL Server; the matrix owns MySQL, which has none.
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

    // learn.microsoft.com REGEXP_LIKE, flags: c, i, m and s. SQL Server 2025 rejects 'n'
    // and 'x', so NewLine and ExcludingWhiteSpace are the gaps; 2022 has no REGEXP_*.
    private static readonly HashSet<string> SqlServerMatchOptions = new(StringComparer.Ordinal)
    {
        "CaseSensitive", "CaseInsensitive", "MultipleLines",
    };

    private static readonly Dictionary<TargetDbms, HashSet<string>> MatchOptionTable = new()
    {
        [TargetDbms.MySql] = MySqlMatchOptions,
        [TargetDbms.Oracle] = OracleMatchOptions,
        [TargetDbms.PostgreSql] = PostgreSqlMatchOptions,
        [TargetDbms.SqlServer] = SqlServerMatchOptions,
    };

    // --- Row counts: where a negative constant is rejected (#529) ---

    // The parameter SQLA0104 reads the row count out of. Offset/OffsetRows are
    // absent by decision, not for want of evidence (#532): an offset goes
    // negative by arithmetic, and this rule reads only a call-site constant.
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

    // --- ROUND scales: where a negative constant is read as 0 (#742) ---

    // The parameter SQLA0104 reads the scale out of.
    internal const string RoundScaleParameterName = "decimals";

    // sqlite.org round(X,Y): a negative Y is taken to be 0, so SQLite 3.50.4 rounds
    // ROUND(75, -1) to 75 where the other four engines give 80 (#742).
    private static readonly HashSet<TargetDbms> NegativeRoundScaleReadAsZero =
        [TargetDbms.Sqlite];

    // --- String operands of +: where a string is not concatenated (#742) ---

    // MySQL 8.0, Oracle XE 21.3.0 and SQLite 3.50.4 add a string beside + as a number,
    // Oracle failing on text that is none; PostgreSQL 16 rejects it, and SQL Server 2022
    // concatenates it.
    private static readonly HashSet<TargetDbms> StringPlusNotConcatenated =
        [TargetDbms.MySql, TargetDbms.Oracle, TargetDbms.PostgreSql, TargetDbms.Sqlite];

    // --- GROUP BY ordinals: where a select-list position is not read as one
    // (#521 item 2) ---

    // The parameter SQLA0104 reads the group keys out of. An ordinal below 1 names
    // no select-list position, so the library rejects that at the call instead.
    internal const string GroupByItemsParameterName = "groupByItems";

    // MySQL 8.0.46, PostgreSQL 16.15 and SQLite 3.50.4 group by the position. SQL
    // Server 2022 refuses every constant key; Oracle XE 21.3.0 and 23.26.3 group by
    // the constant, one group, the other meaning a twin pins (#684).
    private static readonly HashSet<TargetDbms> OrdinalGroupByUnread =
        [TargetDbms.Oracle, TargetDbms.SqlServer];

    // MySQL 8.0.46, Oracle XE 21.3.0 and 23.26.3 and SQLite 3.50.4 read a
    // non-integer constant as one group; PostgreSQL 16.15 and SQL Server 2022
    // reject it (`non-integer constant in GROUP BY` on PostgreSQL).
    private static readonly HashSet<TargetDbms> FractionalGroupKeyRejected =
        [TargetDbms.PostgreSql, TargetDbms.SqlServer];

    // --- CURRENT_TIMESTAMP / CURRENT_TIME precisions: each engine's ceiling (#742) ---

    // The parameter SQLA0104 reads the precision out of.
    internal const string PrecisionParameterName = "precision";

    // MySQL 8.0 rejects 7 on both (error 1426), and Oracle XE 21.3.0 and 23.26.3 reject
    // CURRENT_TIMESTAMP(10) (ORA-30088); PostgreSQL 16.15 runs CURRENT_TIMESTAMP(10).
    private static readonly Dictionary<(string Member, TargetDbms Dbms), int> PrecisionCeiling =
        new()
        {
            [("CurrentTime", TargetDbms.MySql)] = 6,
            [("CurrentTimestamp", TargetDbms.MySql)] = 6,
            [("CurrentTimestamp", TargetDbms.Oracle)] = 9,
        };

    // --- Interval literals: a leading value past Oracle's default precision (#742) ---

    // The parameters SQLA0104 reads the value and the leading field out of, per arity.
    internal const string IntervalValueParameterName = "value";

    internal const string IntervalTrailingFieldParameterName = "trailingField";

    internal static readonly Dictionary<int, string> IntervalLeadingFieldParameterName = new()
    {
        [2] = "field",
        [3] = "leadingField",
    };

    // Oracle XE 21.3.0 and 23.26.3 reject a DAY or YEAR value of three or more digits under no
    // precision (ORA-01873) and run it under one that covers it; '100' MONTH, HOUR, MINUTE
    // and SECOND run under none.
    private static readonly HashSet<string> DefaultPrecisionLeadingFields =
        new(StringComparer.Ordinal) { "Day", "Year" };

    // The trailing fields (factory, declared arity) the core admits after each such lead; any
    // other range throws at the call, so the value is not the problem to name there.
    private static readonly HashSet<(string Leading, string Trailing, int Arity)> AdmittedRanges =
    [
        ("Day", "Hour", 0),
        ("Day", "Minute", 0),
        ("Day", "Second", 0),
        ("Day", "ToHour", 0),
        ("Day", "ToMinute", 0),
        ("Day", "ToSecond", 0),
        ("Day", "ToSecond", 1),
        ("Year", "Month", 0),
        ("Year", "ToMonth", 0),
    ];

    /// <summary>
    /// Whether <paramref name="dbms"/> is measured not to read a <c>GROUP BY</c>
    /// constant as a select-list position.
    /// </summary>
    public static bool ReadsNoGroupByOrdinal(TargetDbms dbms) =>
        OrdinalGroupByUnread.Contains(dbms);

    /// <summary>
    /// Whether <paramref name="dbms"/> is measured not to concatenate a string
    /// operand of <c>+</c>.
    /// </summary>
    public static bool ConcatenatesNoStringPlusOperand(TargetDbms dbms) =>
        StringPlusNotConcatenated.Contains(dbms);

    /// <summary>
    /// Whether <paramref name="dbms"/> is measured to read a negative constant
    /// <c>ROUND</c> scale as 0.
    /// </summary>
    public static bool ReadsNegativeRoundScaleAsZero(TargetDbms dbms) =>
        NegativeRoundScaleReadAsZero.Contains(dbms);

    /// <summary>
    /// Whether <paramref name="dbms"/> is measured to reject a non-integer
    /// constant <c>GROUP BY</c> key.
    /// </summary>
    public static bool RejectsFractionalGroupKey(TargetDbms dbms) =>
        FractionalGroupKeyRejected.Contains(dbms);

    /// <summary>
    /// The highest <paramref name="memberName"/> precision <paramref name="dbms"/> is
    /// measured to accept, or <see langword="null"/> when this rule has none for the pair.
    /// </summary>
    public static int? PrecisionCeilingFor(string memberName, TargetDbms dbms) =>
        PrecisionCeiling.TryGetValue((memberName, dbms), out int ceiling) ? ceiling : null;

    /// <summary>
    /// Whether <paramref name="dbms"/> is measured to reject a value of three or more digits
    /// under the no-precision <paramref name="fieldFactory"/> leading an interval literal.
    /// </summary>
    public static bool RejectsThreeDigitLeadingValue(string fieldFactory, TargetDbms dbms) =>
        dbms == TargetDbms.Oracle && DefaultPrecisionLeadingFields.Contains(fieldFactory);

    /// <summary>
    /// Whether the core admits the trailing field <paramref name="trailingFactory"/>, a factory
    /// of <paramref name="arity"/> declared parameters, after <paramref name="leadingFactory"/>.
    /// </summary>
    public static bool AdmitsRange(string leadingFactory, string trailingFactory, int arity) =>
        AdmittedRanges.Contains((leadingFactory, trailingFactory, arity));

    /// <summary>Every range the interval cell reports on — the parity gate's check.</summary>
    internal static IEnumerable<(string Leading, string Trailing, int Arity)> AllAdmittedRanges =>
        AdmittedRanges;

    /// <summary>Every (member, dialect) cell the precision table holds — the parity
    /// gate's coverage check.</summary>
    internal static IEnumerable<(string Member, TargetDbms Dbms)> AllPrecisionCells =>
        PrecisionCeiling.Keys;

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
