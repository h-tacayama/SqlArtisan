using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>One catalog entry: a builder that exercises a single API construct, and the engines it is valid on.</summary>
/// <param name="Name">Label shown in the smoke report.</param>
/// <param name="Build">Produces the <see cref="ISqlBuilder"/> to execute (always a single-column SELECT).</param>
/// <param name="Engines">The engines the construct's emitted SQL is expected to run on.</param>
public sealed record SmokeCase(string Name, Func<ISqlBuilder> Build, Dbms[] Engines);

/// <summary>
/// An API-coverage catalog: each public <c>Sql.*</c> function is exercised by a
/// single-column <c>SELECT</c> against the seeded schema, tagged with the engines
/// whose grammar accepts it. The per-engine smoke test
/// (<c>IntegrationTestBase.Api_FunctionSmoke</c>) runs every case valid for its
/// engine and aggregates the failures, so one matrix run reports exactly which
/// construct fails on which engine — surfacing "emits but won't execute" bugs
/// (cf. #165, #168) across the whole surface at once.
///
/// Scope note: date/time, conversion (TO_CHAR/TO_DATE/…), regexp, and sequence
/// functions need date columns / sequence DDL and are heavily dialect-specific;
/// they are deferred to a follow-up. CASE and set/clause builders are covered by
/// the dedicated tests, not here.
/// </summary>
internal static class SmokeCatalog
{
    private static readonly Dbms[] All =
        [Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite, Dbms.SqlServer];

    private static Dbms[] Only(params Dbms[] engines) => engines;

    public static IReadOnlyList<SmokeCase> Cases { get; } = Build();

    private static List<SmokeCase> Build()
    {
        UsersTable u = new();
        OrdersTable o = new();
        List<SmokeCase> cases = [];

        void Add(string name, Func<ISqlBuilder> build, Dbms[] engines) =>
            cases.Add(new SmokeCase(name, build, engines));

        // Wrap a scalar expression as `SELECT <expr> FROM users WHERE id = 1`
        // (one row; a FROM is always present so Oracle does not need DUAL).
        ISqlBuilder Scalar(object expr) => Select(expr).From(u).Where(u.Id == 1);
        // Wrap a window expression as `SELECT <expr> FROM users` (all rows).
        ISqlBuilder Window(object expr) => Select(expr).From(u);

        // --- Aggregates (universal) ---
        Add("Avg", () => Select(Avg(o.Amount)).From(o), All);
        Add("Count", () => Select(Count(u.Id)).From(u), All);
        Add("Max", () => Select(Max(u.Age)).From(u), All);
        Add("Min", () => Select(Min(u.Age)).From(u), All);
        Add("Sum", () => Select(Sum(o.Amount)).From(o), All);

        // --- Numeric scalars ---
        Add("Abs", () => Scalar(Abs(-5)), All);
        // Round a numeric column: PostgreSQL has round(numeric, int) but not
        // round(double, int), so a double literal would fail there.
        Add("Round", () => Select(Round(o.Amount, 1)).From(o).Where(o.Id == 1), All);
        Add("Floor", () => Scalar(Floor(1.7)), All);
        Add("Sign", () => Scalar(Sign(-5)), All);
        Add("Ceil", () => Scalar(Ceil(1.2)), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));
        Add("Ceiling", () => Scalar(Ceiling(1.2)), Only(Dbms.MySql, Dbms.PostgreSql, Dbms.Sqlite, Dbms.SqlServer));
        // SQLite has POWER/SQRT when built with math functions (3.35+, enabled in
        // the bundled Microsoft.Data.Sqlite native library).
        Add("Power", () => Scalar(Power(2, 3)), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite, Dbms.SqlServer));
        Add("Sqrt", () => Scalar(Sqrt(16)), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite, Dbms.SqlServer));
        Add("Mod", () => Scalar(Mod(10, 3)), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql));

        // --- String scalars ---
        Add("Upper", () => Scalar(Upper("abc")), All);
        Add("Lower", () => Scalar(Lower("ABC")), All);
        Add("Trim", () => Scalar(Trim(" x ")), All);
        Add("Ltrim", () => Scalar(Ltrim(" x")), All);
        Add("Rtrim", () => Scalar(Rtrim("x ")), All);
        Add("Replace", () => Scalar(Replace("abc", "b", "X")), All);
        Add("Length", () => Scalar(Length("abc")), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));
        Add("Substr", () => Scalar(Substr("abcdef", 2, 3)), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));
        // CONCAT is on SQLite 3.44+ (bundled build), alongside the others.
        Add("Concat", () => Scalar(Concat("a", "b")), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite, Dbms.SqlServer));
        // SQLite has no LPAD/RPAD built-in.
        Add("Lpad", () => Scalar(Lpad("x", 3, "0")), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql));
        Add("Rpad", () => Scalar(Rpad("x", 3, "0")), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql));
        Add("Instr", () => Scalar(Instr("abc", "b")), Only(Dbms.MySql, Dbms.Oracle, Dbms.Sqlite));
        // LENGTHB / SUBSTRB are Oracle byte-semantics builtins.
        Add("Lengthb", () => Scalar(Lengthb("abc")), Only(Dbms.Oracle));
        Add("Substrb", () => Scalar(Substrb("abcdef", 2)), Only(Dbms.Oracle));
        // Numeric TRUNC: Oracle/PostgreSQL/SQLite (MySQL spells it TRUNCATE; SQL Server has no TRUNC).
        Add("Trunc", () => Scalar(Trunc(123.456)), Only(Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));

        // --- Conditional / null handling ---
        Add("Coalesce", () => Scalar(Coalesce(u.Name, "x")), All);
        Add("Nullif", () => Scalar(Nullif(1, 2)), All);
        Add("Exists", () => Select(Count(u.Id)).From(u).Where(Exists(Select(o.Id).From(o))), All);
        Add("NotExists", () => Select(Count(u.Id)).From(u).Where(NotExists(Select(o.Id).From(o).Where(o.Id == -1))), All);
        Add("Cast", () => Scalar(Cast(u.Age, "DECIMAL(10,2)")), All);
        Add("Greatest", () => Scalar(Greatest(1, 2, 3)), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.SqlServer));
        Add("Least", () => Scalar(Least(1, 2, 3)), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.SqlServer));
        Add("Nvl", () => Scalar(Nvl(u.Name, "x")), Only(Dbms.Oracle));
        Add("Decode", () => Scalar(Decode(u.Age, (30, "thirty"), "other")), Only(Dbms.Oracle));

        // --- Analytic / window (universal unless noted) ---
        Add("Rank", () => Window(Rank().Over(OrderBy(u.Age))), All);
        Add("DenseRank", () => Window(DenseRank().Over(OrderBy(u.Age))), All);
        Add("RowNumber", () => Window(RowNumber().Over(OrderBy(u.Age))), All);
        Add("PercentRank", () => Window(PercentRank().Over(OrderBy(u.Age))), All);
        Add("CumeDist", () => Window(CumeDist().Over(OrderBy(u.Age))), All);
        Add("Ntile", () => Window(Ntile(2).Over(OrderBy(u.Age))), All);
        Add("Lag", () => Window(Lag(u.Age).Over(OrderBy(u.Id))), All);
        Add("Lead", () => Window(Lead(u.Age).Over(OrderBy(u.Id))), All);
        Add("FirstValue", () => Window(FirstValue(u.Age).Over(OrderBy(u.Id))), All);
        Add("LastValue", () => Window(LastValue(u.Age).Over(OrderBy(u.Id))), All);
        // SQL Server has no NTH_VALUE.
        Add("NthValue", () => Window(NthValue(u.Age, 2).Over(OrderBy(u.Id))),
            Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));
        Add("PercentileCont", () => Select(PercentileCont(0.5).WithinGroup(OrderBy(u.Age))).From(u),
            Only(Dbms.Oracle, Dbms.PostgreSql));
        Add("PercentileDisc", () => Select(PercentileDisc(0.5).WithinGroup(OrderBy(u.Age))).From(u),
            Only(Dbms.Oracle, Dbms.PostgreSql));

        // --- CASE (universal) ---
        Add("Case", () => Scalar(Case(When(u.Age > 30).Then("old"), Else("young"))), All);

        // --- Niladic date/time functions ---
        // CURRENT_TIMESTAMP is universal; CURRENT_DATE has no SQL Server form;
        // CURRENT_TIME exists only on MySQL / PostgreSQL / SQLite.
        Add("CurrentTimestamp", () => Scalar(CurrentTimestamp), All);
        Add("CurrentDate", () => Scalar(CurrentDate),
            Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));
        Add("CurrentTime", () => Scalar(CurrentTime),
            Only(Dbms.MySql, Dbms.PostgreSql, Dbms.Sqlite));
        // Oracle SYSDATE / SYSTIMESTAMP selected FROM DUAL (also covers the DUAL table).
        Add("Sysdate", () => Select(Sysdate).From(Dual), Only(Dbms.Oracle));
        Add("Systimestamp", () => Select(Systimestamp).From(Dual), Only(Dbms.Oracle));

        // --- Date / time (operate on the created_at column) ---
        Add("Extract", () => Scalar(Extract(DateTimePart.Year, u.CreatedAt)),
            Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql));
        Add("Datepart", () => Scalar(Datepart(DateTimePart.Year, u.CreatedAt)), Only(Dbms.SqlServer));
        Add("Dateadd", () => Scalar(Dateadd(DateTimePart.Day, 1, u.CreatedAt)), Only(Dbms.SqlServer));
        Add("Datediff", () => Scalar(Datediff(DateTimePart.Day, u.CreatedAt, u.CreatedAt)), Only(Dbms.SqlServer));
        Add("DateTrunc", () => Scalar(DateTrunc(DateTimePart.Month, u.CreatedAt)), Only(Dbms.PostgreSql));
        Add("AddMonths", () => Scalar(AddMonths(u.CreatedAt, 1)), Only(Dbms.Oracle));
        Add("LastDay", () => Scalar(LastDay(u.CreatedAt)), Only(Dbms.MySql, Dbms.Oracle));
        Add("MonthsBetween", () => Scalar(MonthsBetween(u.CreatedAt, u.CreatedAt)), Only(Dbms.Oracle));

        // --- Conversion (Oracle / PostgreSQL) ---
        Add("ToChar", () => Scalar(ToChar(u.Age, "999")), Only(Dbms.Oracle, Dbms.PostgreSql));
        // PostgreSQL's to_number requires a format argument (Oracle allows one arg).
        Add("ToNumber", () => Scalar(ToNumber("123", "999")), Only(Dbms.Oracle, Dbms.PostgreSql));
        Add("ToDate", () => Scalar(ToDate("2020-01-01", "YYYY-MM-DD")), Only(Dbms.Oracle, Dbms.PostgreSql));
        Add("ToTimestamp", () => Scalar(ToTimestamp("2020-01-01 00:00:00", "YYYY-MM-DD HH24:MI:SS")),
            Only(Dbms.Oracle, Dbms.PostgreSql));

        // --- Regexp ---
        Add("RegexpLike", () => Select(Count(u.Id)).From(u).Where(RegexpLike(u.Name, "A.*")),
            Only(Dbms.MySql, Dbms.Oracle));
        Add("RegexpReplace", () => Scalar(RegexpReplace(u.Name, "a", "b")),
            Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql));
        Add("RegexpSubstr", () => Scalar(RegexpSubstr(u.Name, "A")), Only(Dbms.MySql, Dbms.Oracle));
        Add("RegexpCount", () => Scalar(RegexpCount(u.Name, "a")), Only(Dbms.Oracle));

        // --- Distinct-arity overloads not exercised by the cases above ---
        // Window LAG/LEAD with an explicit offset and offset+default.
        Add("LagOffset", () => Window(Lag(u.Age, 2).Over(OrderBy(u.Id))), All);
        Add("LagOffsetDefault", () => Window(Lag(u.Age, 2, 0).Over(OrderBy(u.Id))), All);
        Add("LeadOffset", () => Window(Lead(u.Age, 2).Over(OrderBy(u.Id))), All);
        Add("LeadOffsetDefault", () => Window(Lead(u.Age, 2, 0).Over(OrderBy(u.Id))), All);
        // ROUND(expr) with no decimals — SQL Server's ROUND requires the 2nd arg.
        Add("RoundNoDecimals", () => Scalar(Round(1.7)),
            Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));
        // SUBSTR(source, position) — rest of string (no SUBSTR on SQL Server).
        Add("SubstrNoLength", () => Scalar(Substr("abcdef", 2)),
            Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));
        // LPAD/RPAD(source, length) without a pad string — Oracle / PostgreSQL
        // (MySQL's LPAD/RPAD require the pad argument).
        Add("LpadNoPad", () => Scalar(Lpad("x", 3)), Only(Dbms.Oracle, Dbms.PostgreSql));
        Add("RpadNoPad", () => Scalar(Rpad("x", 3)), Only(Dbms.Oracle, Dbms.PostgreSql));
        // LTRIM/RTRIM(source, trimChars) — Oracle / PostgreSQL / SQLite
        // (MySQL's LTRIM/RTRIM take no trim-character argument).
        Add("LtrimChars", () => Scalar(Ltrim("xxabc", "x")),
            Only(Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));
        Add("RtrimChars", () => Scalar(Rtrim("abcxx", "x")),
            Only(Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));
        // SUBSTRB(source, position, length) — Oracle byte-semantics, 3-arg form.
        Add("SubstrbLength", () => Scalar(Substrb("abcdef", 2, 3)), Only(Dbms.Oracle));
        // REGEXP_SUBSTR / REGEXP_COUNT with a start position (and a match option).
        Add("RegexpSubstrPosition", () => Scalar(RegexpSubstr(u.Name, "A", 1)),
            Only(Dbms.MySql, Dbms.Oracle));
        Add("RegexpCountPosition", () => Scalar(RegexpCount(u.Name, "a", 1)), Only(Dbms.Oracle));
        Add("RegexpCountOptions", () => Scalar(RegexpCount(u.Name, "a", 1, RegexpOptions.CaseInsensitive)),
            Only(Dbms.Oracle));
        // DISTINCT inside SUM / AVG.
        Add("SumDistinct", () => Select(Sum(Distinct, o.Amount)).From(o), All);
        Add("AvgDistinct", () => Select(Avg(Distinct, o.Amount)).From(o), All);
        // Ordered string aggregation: PostgreSQL inline ORDER BY, SQL Server
        // WITHIN GROUP, MySQL GROUP_CONCAT ... ORDER BY, SQLite positional separator.
        Add("StringAggOrderBy", () => Select(StringAgg(u.Name, ", ", OrderBy(u.Name))).From(u),
            Only(Dbms.PostgreSql));
        Add("StringAggWithinGroup",
            () => Select(StringAgg(u.Name, ", ").WithinGroup(OrderBy(u.Name))).From(u),
            Only(Dbms.SqlServer));
        Add("GroupConcatOrderBy", () => Select(GroupConcat(u.Name, OrderBy(u.Name))).From(u),
            Only(Dbms.MySql));
        Add("GroupConcatSeparator", () => Select(GroupConcat(u.Name, ", ")).From(u),
            Only(Dbms.Sqlite));

        return cases;
    }
}
