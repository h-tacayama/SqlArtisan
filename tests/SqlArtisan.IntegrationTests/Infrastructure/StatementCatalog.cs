using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// Read-only statement/clause constructs (beyond plain SELECT) exercised against
/// each engine that accepts them — recursive CTE, APPLY/LATERAL, GROUP BY
/// extensions, window frames, FOR UPDATE, NULLS ordering, hints. Same shape and
/// aggregate-reporting runner as <see cref="SmokeCatalog"/>: a per-engine test
/// runs every applicable case and reports which construct fails on which engine.
/// Mutating statements (INSERT … SELECT, multi-row VALUES) are separate
/// transaction-isolated tests.
/// </summary>
internal static class StatementCatalog
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

        // Window frame (ROWS) — all five (MySQL 8, SQLite 3.28+).
        Add("WindowFrame",
            () => Select(Avg(o.Amount).Over(OrderBy(o.Id).Rows(UnboundedPreceding))).From(o), All);

        // GROUP BY extensions — Oracle / PostgreSQL / SQL Server (function form).
        Add("Rollup",
            () => Select(u.DepartmentId).From(u).GroupBy(Rollup(u.DepartmentId)),
            Only(Dbms.Oracle, Dbms.PostgreSql, Dbms.SqlServer));
        Add("Cube",
            () => Select(u.DepartmentId).From(u).GroupBy(Cube(u.DepartmentId)),
            Only(Dbms.Oracle, Dbms.PostgreSql, Dbms.SqlServer));
        Add("GroupingSets",
            () => Select(u.DepartmentId).From(u).GroupBy(GroupingSets(Group(u.DepartmentId), Group())),
            Only(Dbms.Oracle, Dbms.PostgreSql, Dbms.SqlServer));

        // MySQL's GROUP BY ... WITH ROLLUP suffix.
        Add("WithRollup",
            () => Select(u.DepartmentId).From(u).GroupBy(u.DepartmentId).WithRollup(),
            Only(Dbms.MySql));

        // NULLS FIRST/LAST ordering — PostgreSQL / Oracle / SQLite.
        Add("NullsLast",
            () => Select(u.Id).From(u).OrderBy(u.Age.NullsLast),
            Only(Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));

        // Optimizer-hint comment — a /*+ ... */ comment is accepted everywhere.
        Add("Hints", () => Select(Hints("/*+ test */"), u.Id).From(u), All);

        // FOR UPDATE row locking — PostgreSQL / MySQL / Oracle.
        Add("ForUpdate",
            () => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate(),
            Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql));

        // Recursive CTE (WITH RECURSIVE) — PostgreSQL / SQLite / MySQL; Oracle and
        // SQL Server use WITH without the RECURSIVE keyword.
        Add("RecursiveCte", () =>
        {
            UsersTable ru = new("ru");
            Cte c = new("c");
            return WithRecursive(
                    c.As(
                        Select(ru.Id.As(c.Column("id"))).From(ru).Where(ru.Id == 1)
                        .UnionAll
                        .Select(ru.Id).From(ru).InnerJoin(c).On(ru.Id == c.Column("id") + 1)
                        .Where(ru.Id <= 3)))
                .Select(c.Column("id")).From(c);
        }, Only(Dbms.PostgreSql, Dbms.Sqlite, Dbms.MySql));

        // CROSS APPLY — SQL Server / Oracle.
        Add("CrossApply", () =>
        {
            UsersTable au = new("u");
            OrdersTable ao = new("o");
            DerivedTable x = new("x");
            return Select(au.Id, x.Column("amount")).From(au)
                .CrossApply(
                    Select(ao.Amount.As(x.Column("amount"))).From(ao).Where(ao.UserId == au.Id),
                    x);
        }, Only(Dbms.Oracle, Dbms.SqlServer));

        // JOIN LATERAL — PostgreSQL / MySQL.
        Add("JoinLateral", () =>
        {
            UsersTable lu = new("u");
            OrdersTable lo = new("o");
            DerivedTable x = new("x");
            return Select(lu.Id, x.Column("amount")).From(lu)
                .JoinLateral(
                    Select(lo.Amount.As(x.Column("amount"))).From(lo).Where(lo.UserId == lu.Id),
                    x)
                .On(lu.Id == lu.Id);
        }, Only(Dbms.PostgreSql, Dbms.MySql));

        return cases;
    }
}
