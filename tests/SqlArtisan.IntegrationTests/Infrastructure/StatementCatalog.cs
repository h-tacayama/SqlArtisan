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
        // The rest of the frame vocabulary: BETWEEN bounds and RANGE, plus the
        // Preceding(n) / Following(n) / CurrentRow / UnboundedFollowing bounds.
        Add("WindowFrameRowsBetween",
            () => Select(Avg(o.Amount).Over(OrderBy(o.Id).RowsBetween(Preceding(1), Following(1)))).From(o), All);
        Add("WindowFrameRangeBetween",
            () => Select(Avg(o.Amount).Over(OrderBy(o.Id).RangeBetween(UnboundedPreceding, CurrentRow))).From(o), All);
        Add("WindowFrameUnboundedFollowing",
            () => Select(Avg(o.Amount).Over(OrderBy(o.Id).RowsBetween(CurrentRow, UnboundedFollowing))).From(o), All);
        Add("WindowFrameRange",
            () => Select(Avg(o.Amount).Over(OrderBy(o.Id).Range(UnboundedPreceding))).From(o), All);

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
        Add("NullsFirst",
            () => Select(u.Id).From(u).OrderBy(u.Age.NullsFirst),
            Only(Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite));

        // LIKE ... ESCAPE — the escape char is inlined as a literal; valid on all five.
        Add("LikeEscape",
            () => Select(u.Id).From(u).Where(u.Name.Like("A!%").Escape('!')), All);
        // A backslash escape char exercises MySQL's literal backslash-doubling
        // branch (BackslashEscapesStringLiterals); a bare backslash on the others.
        Add("LikeEscapeBackslash",
            () => Select(u.Id).From(u).Where(u.Name.Like("A\\%").Escape('\\')), All);

        // Pagination clauses used standalone: LIMIT without OFFSET (MySQL /
        // PostgreSQL / SQLite) and OFFSET without LIMIT (PostgreSQL only — MySQL
        // and SQLite require a LIMIT before OFFSET).
        Add("LimitStandalone", () => Select(u.Id).From(u).OrderBy(u.Id).Limit(2),
            Only(Dbms.MySql, Dbms.PostgreSql, Dbms.Sqlite));
        Add("OffsetStandalone", () => Select(u.Id).From(u).OrderBy(u.Id).Offset(2),
            Only(Dbms.PostgreSql));

        // Optimizer-hint comment — a /*+ ... */ comment is accepted everywhere.
        Add("Hints", () => Select(Hints("/*+ test */"), u.Id).From(u), All);

        // Explicit ascending order (the default direction made explicit).
        Add("Asc", () => Select(u.Id).From(u).OrderBy(u.Id.Asc), All);

        // FETCH FIRST n ROWS ONLY — Oracle 12c+ / PostgreSQL (SQL Server needs a
        // preceding OFFSET, so it is covered by OffsetRows().FetchNext() instead).
        Add("FetchFirst", () => Select(u.Id).From(u).OrderBy(u.Id).FetchFirst(2),
            Only(Dbms.Oracle, Dbms.PostgreSql));

        // Set operators with ALL — EXCEPT ALL (PostgreSQL / MySQL 8.0.31+) and
        // Oracle's MINUS ALL.
        Add("ExceptAll",
            () => Select(u.DepartmentId).From(u).ExceptAll.Select(u.DepartmentId).From(u),
            Only(Dbms.MySql, Dbms.PostgreSql));
        Add("MinusAll",
            () => Select(u.DepartmentId).From(u).MinusAll.Select(u.DepartmentId).From(u),
            Only(Dbms.Oracle));

        // FOR UPDATE lock-wait behaviours and FOR UPDATE OF.
        Add("ForUpdateNowait",
            () => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate(Nowait),
            Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql));
        Add("ForUpdateSkipLocked",
            () => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate(SkipLocked),
            Only(Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql));
        Add("ForUpdateWait",
            () => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate(Wait(3)),
            Only(Dbms.Oracle));
        Add("ForUpdateOf",
            () => Select(u.Id).From(u).Where(u.Id == 1).ForUpdate(Of(u.Id)),
            Only(Dbms.Oracle));

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

        // OUTER joins beyond INNER — RIGHT (all five; SQLite 3.39+) and FULL
        // (every engine except MySQL, which has no FULL JOIN). Both tables are
        // aliased so the join columns stay qualified (id exists on both).
        Add("RightJoin", () =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).RightJoin(ju).On(jo.UserId == ju.Id);
        }, All);
        Add("FullJoin", () =>
        {
            UsersTable ju = new("u");
            OrdersTable jo = new("o");
            return Select(jo.Amount).From(jo).FullJoin(ju).On(jo.UserId == ju.Id);
        }, Only(Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite, Dbms.SqlServer));

        // Set operators not covered by the dedicated UNION/EXCEPT tests:
        // INTERSECT (all five; MySQL 8.0.31+) and INTERSECT ALL (PostgreSQL / MySQL).
        Add("Intersect",
            () => Select(u.DepartmentId).From(u).Intersect.Select(u.DepartmentId).From(u), All);
        Add("IntersectAll",
            () => Select(u.DepartmentId).From(u).IntersectAll.Select(u.DepartmentId).From(u),
            Only(Dbms.MySql, Dbms.PostgreSql));

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

        // OUTER APPLY — the sibling of CROSS APPLY (SQL Server / Oracle).
        Add("OuterApply", () =>
        {
            UsersTable au = new("u");
            OrdersTable ao = new("o");
            DerivedTable x = new("x");
            return Select(au.Id, x.Column("amount")).From(au)
                .OuterApply(
                    Select(ao.Amount.As(x.Column("amount"))).From(ao).Where(ao.UserId == au.Id),
                    x);
        }, Only(Dbms.Oracle, Dbms.SqlServer));

        // CROSS / LEFT JOIN LATERAL — the siblings of JOIN LATERAL (PostgreSQL / MySQL).
        Add("CrossJoinLateral", () =>
        {
            UsersTable lu = new("u");
            OrdersTable lo = new("o");
            DerivedTable x = new("x");
            return Select(lu.Id, x.Column("amount")).From(lu)
                .CrossJoinLateral(
                    Select(lo.Amount.As(x.Column("amount"))).From(lo).Where(lo.UserId == lu.Id),
                    x);
        }, Only(Dbms.PostgreSql, Dbms.MySql));
        Add("LeftJoinLateral", () =>
        {
            UsersTable lu = new("u");
            OrdersTable lo = new("o");
            DerivedTable x = new("x");
            return Select(lu.Id, x.Column("amount")).From(lu)
                .LeftJoinLateral(
                    Select(lo.Amount.As(x.Column("amount"))).From(lo).Where(lo.UserId == lu.Id),
                    x);
        }, Only(Dbms.PostgreSql, Dbms.MySql));

        return cases;
    }
}
