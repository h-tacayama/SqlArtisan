using System.Data;
using Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

// Throwaway observation probe for #679's last two items (never merged): each lane
// records what its engine does and fails on purpose so the log carries the record.
internal static class Probe679b
{
    public static string Q(IDbConnection c, string label, string sql, IDbTransaction? tx = null)
    {
        try
        {
            IEnumerable<string> rows = c.Query(sql, transaction: tx).Select(r => string.Join(
                ",",
                ((IDictionary<string, object>)r).Values.Select(v => v is null ? "NULL" : $"[{v}]")));
            return $"{label}: ok: {string.Join(" | ", rows)}";
        }
        catch (Exception e)
        {
            return $"{label}: ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}";
        }
    }

    public static string X(IDbConnection c, string label, string sql, IDbTransaction? tx = null)
    {
        try
        {
            return $"{label}: ok, {c.Execute(sql, transaction: tx)} row(s)";
        }
        catch (Exception e)
        {
            return $"{label}: ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}";
        }
    }

    public static void Report(string engine, IEnumerable<string> lines) =>
        Assert.Fail($"PROBE679B {engine}\n" + string.Join("\n", lines) + "\nEND PROBE679B");
}

[Trait("Probe", "MySql")]
public sealed class MySqlProbe679b : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _f;

    public MySqlProbe679b(MySqlFixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe()
    {
        List<string> log = new();
        using IDbConnection c = _f.OpenConnection();
        log.Add(Probe679b.Q(c, "version", "SELECT VERSION(), @@sql_mode"));
        log.Add(Probe679b.Q(c, "rows", "SELECT COUNT(*) FROM users u JOIN orders o ON u.id = o.user_id"));

        const string Head =
            "SELECT u.id FROM users u INNER JOIN orders o ON u.id = o.user_id ORDER BY u.id ";
        (string Label, string Tail)[] shapes =
        [
            ("one.forUpdate", "FOR UPDATE"),
            ("two.updateOfNowait.updateOfSkip", "FOR UPDATE OF u NOWAIT FOR UPDATE OF o SKIP LOCKED"),
            ("two.updateOf.shareOf", "FOR UPDATE OF u FOR SHARE OF o"),
            ("two.updateOf.updateOf", "FOR UPDATE OF u FOR UPDATE OF o"),
            ("two.plain.plain", "FOR UPDATE FOR UPDATE"),
            ("two.plain.shareOf", "FOR UPDATE FOR SHARE OF o"),
            ("two.sameTable", "FOR UPDATE OF u FOR UPDATE OF u"),
            ("one.updateOfBoth", "FOR UPDATE OF u, o"),
        ];
        foreach ((string label, string tail) in shapes)
        {
            using IDbTransaction tx = c.BeginTransaction();
            log.Add(Probe679b.Q(c, label, Head + tail, tx));
            tx.Rollback();
        }

        Probe679b.Report("MySql", log);
    }
}

[Trait("Probe", "SqlServer")]
public sealed class SqlServerProbe679b : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _f;

    public SqlServerProbe679b(SqlServerFixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe() => SqlServerProbe679bBody.Run(_f.OpenConnection(), "SqlServer");
}

[Trait("Probe", "SqlServer2025")]
public sealed class SqlServer2025Probe679b : IClassFixture<SqlServer2025Fixture>
{
    private readonly SqlServer2025Fixture _f;

    public SqlServer2025Probe679b(SqlServer2025Fixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe() => SqlServerProbe679bBody.Run(_f.OpenConnection(), "SqlServer2025");
}

internal static class SqlServerProbe679bBody
{
    public static void Run(IDbConnection connection, string engine)
    {
        List<string> log = new();
        using IDbConnection c = connection;
        log.Add(Probe679b.Q(c, "version", "SELECT @@VERSION"));
        log.Add(Probe679b.X(c, "create", "CREATE TABLE p679 (id INT)"));

        (string Label, string Sql)[] shapes =
        [
            ("output.innerWith",
                "INSERT INTO p679 (id) OUTPUT INSERTED.id WITH c AS (SELECT 7 AS id) SELECT id FROM c"),
            ("noOutput.innerWith",
                "INSERT INTO p679 (id) WITH c AS (SELECT 7 AS id) SELECT id FROM c"),
            ("output.leadingWith",
                "WITH c AS (SELECT 7 AS id) INSERT INTO p679 (id) OUTPUT INSERTED.id SELECT id FROM c"),
            ("output.innerWithParenthesized",
                "INSERT INTO p679 (id) OUTPUT INSERTED.id (WITH c AS (SELECT 7 AS id) SELECT id FROM c)"),
        ];
        foreach ((string label, string sql) in shapes)
        {
            using IDbTransaction tx = c.BeginTransaction();
            log.Add(Probe679b.Q(c, label, sql, tx));
            tx.Rollback();
        }

        log.Add(Probe679b.X(c, "drop", "DROP TABLE p679"));
        Probe679b.Report(engine, log);
    }
}
