using System.Data;
using System.Runtime.CompilerServices;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <param name="Code">The unique key the upsert tests arbitrate on beside the id; no query of
/// the SELECT lane reads it.</param>
internal sealed record UserRow(long Id, string? Name, long? Age, long? DeptId, long? Code = null);

internal sealed record DeptRow(long Id, string? Name);

/// <summary>
/// The combinatorial tests' own two tables, apart from the baseline <c>users</c>/<c>orders</c>
/// seed. Every text value stays distinct ignoring case and trailing spaces, and none holds an
/// uppercase <c>O</c>, so the case-insensitive collations agree with the ordinal reference.
/// </summary>
/// <remarks>The unique, nullable <c>rt_users.code</c> is <c>100 + id</c>, NULL where the id is a
/// multiple of 7, apart from every code a statement writes.</remarks>
internal static class RandomSchema
{
    private static readonly UserRow[] s_users =
    [
        new(1, "Alice", 30, 1), new(2, "Ann", 25, 1), new(3, "Bob", 30, 2),
        new(4, "Bob", null, 2), new(5, null, 40, 3), new(6, "Carol", 20, null),
        new(7, "Dave", -5, 99), new(8, "Eve", 30, 4), new(9, "A_b", 0, 5),
        new(10, "50%", 10, 5), new(11, "Zed", null, null), new(12, null, null, 1),
        new(13, "Al", 3, 2), new(14, "Alice", 25, 3), new(15, "Eve", 99, 99),
        new(16, "Ivy", 1, 1), new(17, "Dave", 20, 2), new(18, "Carol", 40, null),
        new(19, "Bob", 5, 5), new(20, "Bo", 2, 4), new(21, "Zed", 30, 1),
        new(22, null, 25, null), new(23, "Ivy", 30, 3), new(24, "Ann", 1, 2),
        new(25, "Alice", null, 99), new(26, "Al", 7, 1), new(27, "Eve", 30, null),
        new(28, "Bo", 10, 3),
    ];

    public static readonly UserRow[] Users =
        [.. s_users.Select(r => r with { Code = r.Id % 7 == 0 ? null : 100 + r.Id })];

    public static readonly DeptRow[] Depts =
    [
        new(1, "Sales"), new(2, "Eng"), new(3, "Eng"), new(4, null), new(5, "Hr"), new(6, "Legal"),
    ];

    private static readonly ConditionalWeakTable<IDatabaseFixture, object> s_created = [];

    /// <summary>Creates and seeds the tables once per fixture, whichever test reaches it
    /// first.</summary>
    public static void EnsureCreated(IDatabaseFixture fixture)
    {
        lock (s_created)
        {
            if (s_created.TryGetValue(fixture, out _))
            {
                return;
            }

            using IDbConnection connection = fixture.OpenConnection();
            Apply(connection, fixture.Dbms);
            s_created.Add(fixture, new object());
        }
    }

    public static void Apply(IDbConnection connection, Dbms dbms)
    {
        foreach (string statement in Ddl(dbms))
        {
            connection.Execute(statement);
        }

        RandomDeptsTable d = new("");
        foreach (DeptRow row in Depts)
        {
            connection.Execute(InsertInto(d, d.Id, d.Name).Values(row.Id, Value(row.Name)));
        }

        RandomUsersTable u = new("");
        foreach (UserRow row in Users)
        {
            connection.Execute(
                InsertInto(u, u.Id, u.Name, u.Age, u.DeptId, u.Code).Values(
                    row.Id,
                    Value(row.Name),
                    Value(row.Age),
                    Value(row.DeptId),
                    Value(row.Code)));
        }
    }

    private static object Value(object? value) => value ?? Null;

    // SQL Server's UNIQUE takes one NULL, so its code key is a unique index over the non-NULL
    // values, which the other engines' UNIQUE is.
    private static string[] Ddl(Dbms dbms) => dbms switch
    {
        Dbms.Oracle =>
        [
            "CREATE TABLE rt_depts (id NUMBER(10) PRIMARY KEY, name VARCHAR2(40))",
            "CREATE TABLE rt_users (id NUMBER(10) PRIMARY KEY, name VARCHAR2(40), "
                + "age NUMBER(10), dept_id NUMBER(10), code NUMBER(10) UNIQUE)",
        ],
        Dbms.SqlServer =>
        [
            "CREATE TABLE rt_depts (id INTEGER PRIMARY KEY, name NVARCHAR(40))",
            "CREATE TABLE rt_users (id INTEGER PRIMARY KEY, name NVARCHAR(40), age INTEGER, "
                + "dept_id INTEGER, code INTEGER)",
            "CREATE UNIQUE INDEX ux_rt_users_code ON rt_users (code) WHERE code IS NOT NULL",
        ],
        _ =>
        [
            "CREATE TABLE rt_depts (id INTEGER PRIMARY KEY, name VARCHAR(40))",
            "CREATE TABLE rt_users (id INTEGER PRIMARY KEY, name VARCHAR(40), age INTEGER, "
                + "dept_id INTEGER, code INTEGER UNIQUE)",
        ],
    };
}

internal sealed class RandomUsersTable : DbTableBase
{
    public RandomUsersTable(string alias) : base("rt_users", alias)
    {
        Id = new DbColumn(this, "id");
        Name = new DbColumn(this, "name");
        Age = new DbColumn(this, "age");
        DeptId = new DbColumn(this, "dept_id");
        Code = new DbColumn(this, "code");
    }

    public DbColumn Id { get; }

    public DbColumn Name { get; }

    public DbColumn Age { get; }

    public DbColumn DeptId { get; }

    public DbColumn Code { get; }
}

internal sealed class RandomDeptsTable : DbTableBase
{
    public RandomDeptsTable(string alias) : base("rt_depts", alias)
    {
        Id = new DbColumn(this, "id");
        Name = new DbColumn(this, "name");
    }

    public DbColumn Id { get; }

    public DbColumn Name { get; }
}
