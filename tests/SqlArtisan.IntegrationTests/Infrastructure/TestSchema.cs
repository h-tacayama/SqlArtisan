using System.Data;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// The shared test schema (two small tables) plus the baseline seed data, applied
/// by every fixture after the engine is up. DDL is inherently dialectal — SqlArtisan
/// does not build DDL — so the <c>CREATE TABLE</c> text lives here, per engine. The
/// seed rows, by contrast, are inserted through SqlArtisan itself, so seeding doubles
/// as INSERT-execution coverage on every engine.
/// </summary>
internal static class TestSchema
{
    // INTEGER / VARCHAR / DECIMAL are accepted verbatim by PostgreSQL, MySQL,
    // SQLite, and SQL Server (INTEGER is a SQL Server synonym for INT).
    // Used by MySQL and SQLite, which have no sequences. INTEGER / VARCHAR /
    // DECIMAL are accepted verbatim across these engines.
    // The trailing `data` column carries a JSON document, exercised by the JSON
    // functions/operators. MySQL needs its native JSON type for the `->`/`->>`
    // operators; SQLite accepts the type name and stores the text (JSON functions
    // operate on it regardless of declared affinity).
    public static readonly string[] StandardDdl =
    [
        "CREATE TABLE users (id INTEGER PRIMARY KEY, name VARCHAR(100), age INTEGER, department_id INTEGER, created_at TIMESTAMP, is_active BOOLEAN, data JSON)",
        "CREATE TABLE orders (id INTEGER PRIMARY KEY, user_id INTEGER, amount DECIMAL(10,2))",
    ];

    // PostgreSQL = the standard tables plus a sequence (exercised by NEXTVAL/CURRVAL).
    // `data` is JSONB so the `->`/`->>`/`#>`/`#>>` operators apply to the column.
    public static readonly string[] PostgreSqlDdl =
    [
        "CREATE TABLE users (id INTEGER PRIMARY KEY, name VARCHAR(100), age INTEGER, department_id INTEGER, created_at TIMESTAMP, is_active BOOLEAN, data JSONB)",
        "CREATE TABLE orders (id INTEGER PRIMARY KEY, user_id INTEGER, amount DECIMAL(10,2))",
        "CREATE SEQUENCE test_seq",
    ];

    // SQL Server: NVARCHAR so Unicode text round-trips (its VARCHAR is non-Unicode); DATETIME2 for the timestamp.
    // JSON_VALUE/JSON_QUERY read JSON out of the NVARCHAR(MAX) `data` column.
    public static readonly string[] SqlServerDdl =
    [
        "CREATE TABLE users (id INTEGER PRIMARY KEY, name NVARCHAR(100), age INTEGER, department_id INTEGER, created_at DATETIME2, is_active BIT, data NVARCHAR(MAX))",
        "CREATE TABLE orders (id INTEGER PRIMARY KEY, user_id INTEGER, amount DECIMAL(10,2))",
        "CREATE SEQUENCE test_seq START WITH 1 INCREMENT BY 1",
    ];

    // Oracle spells the same shapes NUMBER / VARCHAR2 / DATE.
    // JSON_VALUE/JSON_QUERY read JSON out of the VARCHAR2 `data` column.
    public static readonly string[] OracleDdl =
    [
        // Oracle XE 21c (the Testcontainers image) has no native BOOLEAN column
        // type, so the conventional NUMBER(1) stands in. The boolean round-trip
        // test is skipped on Oracle accordingly.
        "CREATE TABLE users (id NUMBER(10) PRIMARY KEY, name VARCHAR2(100), age NUMBER(10), department_id NUMBER(10), created_at DATE, is_active NUMBER(1), data VARCHAR2(4000))",
        "CREATE TABLE orders (id NUMBER(10) PRIMARY KEY, user_id NUMBER(10), amount NUMBER(10,2))",
        "CREATE SEQUENCE test_seq",
    ];

    private static readonly (int Id, string Name, int Age, int DepartmentId)[] s_users =
    [
        (1, "Alice", 30, 10),
        (2, "Bob", 40, 10),
        (3, "Carol", 50, 20),
        (4, "Dave", 25, 20),
        (5, "Eve", 35, 30),
    ];

    private static readonly (int Id, int UserId, decimal Amount)[] s_orders =
    [
        (1, 1, 100.00m),
        (2, 1, 200.00m),
        (3, 2, 50.00m),
        (4, 3, 300.00m),
        (5, 5, 75.00m),
    ];

    // JSON document per user, seeded into the `data` column. Contains a scalar
    // (`name`) and a nested object (`address`) so both scalar extraction
    // (JSON_EXTRACT / JSON_VALUE / ->> / #>>) and object extraction
    // (JSON_QUERY / -> / #>) have something to read. The literals are free of
    // single quotes, so they seed via a plain SQL string literal on every engine
    // (PostgreSQL coerces the unknown literal to JSONB on assignment).
    private static readonly (int Id, string Json)[] s_userJson =
    [
        (1, """{"name": "Alice", "city": "NYC", "address": {"zip": "10001"}}"""),
        (2, """{"name": "Bob", "city": "LA", "address": {"zip": "90001"}}"""),
    ];

    /// <summary>Creates the tables (using <paramref name="ddl"/>) and seeds the baseline rows.</summary>
    public static void Apply(IDbConnection connection, string[] ddl)
    {
        foreach (string statement in ddl)
        {
            connection.Execute(statement);
        }

        UsersTable users = new();
        DateTime createdAt = new(2020, 3, 15, 10, 30, 0);
        foreach ((int id, string name, int age, int departmentId) in s_users)
        {
            connection.Execute(
                InsertInto(users, users.Id, users.Name, users.Age, users.DepartmentId, users.CreatedAt)
                    .Values(id, name, age, departmentId, createdAt));
        }

        OrdersTable orders = new();
        foreach ((int id, int userId, decimal amount) in s_orders)
        {
            connection.Execute(
                InsertInto(orders, orders.Id, orders.UserId, orders.Amount)
                    .Values(id, userId, amount));
        }

        // The JSON column is seeded with a raw literal UPDATE rather than through
        // SqlArtisan: a bound string parameter would not implicitly cast to
        // PostgreSQL's JSONB column, whereas a plain SQL literal coerces on every
        // engine. The document contains no single quotes, so no escaping is needed.
        foreach ((int id, string json) in s_userJson)
        {
            connection.Execute($"UPDATE users SET data = '{json}' WHERE id = {id}");
        }
    }
}
