using System.Data;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

// SQLA0102 (#822): the compounds and positions SQL Server's binding of an embedded compound's
// ORDER BY and paging is measured with, run on both SQL Server lanes.
internal static class EmbeddedCompounds
{
    private static readonly string[] SetOperators = ["Union", "UnionAll", "Intersect", "Except"];

    private static readonly string[] SubqueryPositions =
    [
        "in", "notin", "exists", "notexists", "any", "all", "some",
        "derived", "cte", "scalar", "crossapply", "outerapply",
    ];

    public static TheoryData<string, string> EveryPosition => Build(SubqueryPositions);

    // A subquery, two table positions and a scalar, for the 2025 lane's re-run.
    public static TheoryData<string, string> SomePositions =>
        Build(["in", "derived", "cte", "scalar"]);

    // INTERSECT keeps the 1 only while the ids' page holds a 1; the others page past the end.
    public static int PageOffset(string setOperator) => setOperator == "Intersect" ? 1 : 6;

    public static ISelectBuilderPaginated PagedCompound(string setOperator, int offset) =>
        UserOneThenOrderUserIds(setOperator).OrderBy(1).OffsetRows(offset).FetchNext(1);

    public static ISelectBuilderPaginated PagedCompoundWrappedFirst(
        string setOperator,
        int offset)
    {
        SubqueryDerivedTable w = UserOneThenOrderUserIds(setOperator).AsTable("w");
        return Select(w.Column("id"))
            .From(w)
            .OrderBy(w.Column("id"))
            .OffsetRows(offset)
            .FetchNext(1);
    }

    // User 1, then every order's user id: 1, 1, 2, 3, 5.
    public static ISelectBuilderFrom UserOneThenOrderUserIds(string setOperator)
    {
        UsersTable a = new("a");
        OrdersTable o = new("o");
        return Combine(Select(a.Id).From(a).Where(a.Id == 1), setOperator)
            .Select(o.UserId)
            .From(o);
    }

    // Users 1 to 5, then orders' user ids 1, 1, 2.
    public static ISelectBuilderWhere UsersThenLowOrderUserIds(string setOperator)
    {
        UsersTable a = new("a");
        OrdersTable o = new("o");
        return Combine(Select(a.Id).From(a), setOperator)
            .Select(o.UserId)
            .From(o)
            .Where(o.UserId <= 2);
    }

    public static ISqlBuilder UsersThenLowOrderUserIdsWrappedFirst(
        string setOperator,
        string wrap)
    {
        if (wrap == "cte")
        {
            Cte w = new("w");
            return With(w.As(UsersThenLowOrderUserIds(setOperator)))
                .Select(w.Column("id"))
                .From(w)
                .OrderBy(w.Column("id"))
                .OffsetRows(1)
                .FetchNext(2);
        }

        SubqueryDerivedTable d = UsersThenLowOrderUserIds(setOperator).AsTable("d");
        return Select(d.Column("id"))
            .From(d)
            .OrderBy(d.Column("id"))
            .OffsetRows(1)
            .FetchNext(2);
    }

    // The second branch selects no row, so the scalar spot gets one row under either binding.
    public static ISelectBuilderWhere UserOneThenNoOrderUserId(string setOperator)
    {
        UsersTable a = new("a");
        OrdersTable o = new("o");
        return Combine(Select(a.Id).From(a).Where(a.Id == 1), setOperator)
            .Select(o.UserId)
            .From(o)
            .Where(o.Id == 0);
    }

    public static ISqlBuilder InPosition(string position, UsersTable u, ISubquery query)
    {
        Cte q = new("q");
        return position switch
        {
            "in" => Select(Count(Asterisk)).From(u).Where(u.Id.In(query)),
            "notin" => Select(Count(Asterisk)).From(u).Where(u.Id.NotIn(query)),
            "exists" => Select(Count(Asterisk)).From(u).Where(Exists(query)),
            "notexists" => Select(Count(Asterisk)).From(u).Where(NotExists(query)),
            "any" => Select(Count(Asterisk)).From(u).Where(u.Id > Any(query)),
            "all" => Select(Count(Asterisk)).From(u).Where(u.Id > All(query)),
            "some" => Select(Count(Asterisk)).From(u).Where(u.Id > Some(query)),
            "derived" => Select(Count(Asterisk)).From(query.AsTable("d")),
            "cte" => With(q.As(query)).Select(Count(Asterisk)).From(q),
            "crossapply" =>
                Select(Count(Asterisk)).From(u).CrossApply(query, new DerivedTable("x")),
            "outerapply" =>
                Select(Count(Asterisk)).From(u).OuterApply(query, new DerivedTable("x")),
            _ => Select(query).From(u).Where(u.Id == 1),
        };
    }

    // InPosition's spots selecting the rows they keep: its count cannot tell an empty
    // OUTER APPLY operand from a one-row one.
    public static ISqlBuilder ValuesInPosition(string position, ISubquery query)
    {
        UsersTable u = new("u");
        Cte q = new("q");
        DerivedTable x = new("x");
        return position switch
        {
            "in" => Select(u.Id).From(u).Where(u.Id.In(query)),
            "notin" => Select(u.Id).From(u).Where(u.Id.NotIn(query)),
            "exists" => Select(u.Id).From(u).Where(Exists(query)),
            "notexists" => Select(u.Id).From(u).Where(NotExists(query)),
            "any" => Select(u.Id).From(u).Where(u.Id > Any(query)),
            "all" => Select(u.Id).From(u).Where(u.Id > All(query)),
            "some" => Select(u.Id).From(u).Where(u.Id > Some(query)),
            "derived" => Select(Asterisk).From(query.AsTable("d")),
            "cte" => With(q.As(query)).Select(Asterisk).From(q),
            "scalar" => Select(query).From(u).Where(u.Id == 1),
            "crossapply" => Select(x.Column("id")).From(u).CrossApply(query, x).Where(u.Id == 1),
            "outerapply" => Select(x.Column("id")).From(u).OuterApply(query, x).Where(u.Id == 1),
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
        };
    }

    // ValuesInPosition's rows from users 1 to 5 when the operand is empty (the whole
    // compound paged) and when it is {1} (only the last branch paged).
    public static (string Whole, string LastBranch) PagedCompoundRows(string position) =>
        position switch
        {
            "in" or "derived" or "cte" or "crossapply" => ("[]", "[1]"),
            "notin" => ("[1, 2, 3, 4, 5]", "[2, 3, 4, 5]"),
            "exists" => ("[]", "[1, 2, 3, 4, 5]"),
            "notexists" => ("[1, 2, 3, 4, 5]", "[]"),
            "any" or "some" => ("[]", "[2, 3, 4, 5]"),
            "all" => ("[1, 2, 3, 4, 5]", "[2, 3, 4, 5]"),
            "scalar" or "outerapply" => ("[NULL]", "[1]"),
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
        };

    // The sorted rows, or the engine's Msg number, so a failed assertion prints what ran.
    public static string Outcome(IDbConnection connection, ISqlBuilder query)
    {
        try
        {
            IEnumerable<int?> rows = connection.Query<int?>(query).Order();
            return $"[{string.Join(", ", rows.Select(r => r?.ToString() ?? "NULL"))}]";
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return $"Msg {ex.Number}";
        }
    }

    private static ISelectBuilderSetOperator Combine(ISetOperator first, string setOperator) =>
        setOperator switch
        {
            "Union" => first.Union,
            "UnionAll" => first.UnionAll,
            "Intersect" => first.Intersect,
            "Except" => first.Except,
            _ => throw new ArgumentOutOfRangeException(nameof(setOperator), setOperator, null),
        };

    private static TheoryData<string, string> Build(string[] positions)
    {
        TheoryData<string, string> data = new();
        foreach (string setOperator in SetOperators)
        {
            foreach (string position in positions)
            {
                data.Add(setOperator, position);
            }
        }

        return data;
    }
}
