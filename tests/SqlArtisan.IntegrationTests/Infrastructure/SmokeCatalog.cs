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
        Add("Power", () => Scalar(Power(2, 3)), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.SqlServer));
        Add("Sqrt", () => Scalar(Sqrt(16)), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.SqlServer));
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
        Add("Concat", () => Scalar(Concat("a", "b")), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.SqlServer));
        // SQLite has no LPAD/RPAD built-in.
        Add("Lpad", () => Scalar(Lpad("x", 3, "0")), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql));
        Add("Rpad", () => Scalar(Rpad("x", 3, "0")), Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql));
        Add("Instr", () => Scalar(Instr("abc", "b")), Only(Dbms.MySql, Dbms.Oracle, Dbms.Sqlite));

        // --- Conditional / null handling ---
        Add("Coalesce", () => Scalar(Coalesce(u.Name, "x")), All);
        Add("Nullif", () => Scalar(Nullif(1, 2)), All);
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

        return cases;
    }
}
