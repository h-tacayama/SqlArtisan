using System.Data;
using Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

// Throwaway observation probe for #679 PR (c) (never merged): each lane records
// what its engine does and fails on purpose so the log carries the record.
internal static class Probe679c
{
    public static string Q(IDbConnection c, string label, string sql, IDbTransaction? tx = null)
    {
        try
        {
            IEnumerable<string> rows = c.Query(sql, transaction: tx).Select(r => string.Join(
                ",",
                ((IDictionary<string, object>)r).Values.Select(v => v is null ? "NULL" : $"[{v}]")));
            return $"{label}: {string.Join(" | ", rows)}";
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

    // Each statement in its own rolled-back transaction, with the rows it left.
    public static void Each(
        IDbConnection c, List<string> log, string count, params (string Label, string Sql)[] runs)
    {
        foreach ((string label, string sql) in runs)
        {
            using IDbTransaction tx = c.BeginTransaction();
            log.Add(X(c, label, sql, tx));
            log.Add(Q(c, label + ".left", count, tx));
            tx.Rollback();
        }
    }

    public static void Report(string engine, IEnumerable<string> lines) =>
        Assert.Fail($"PROBE679C {engine}\n" + string.Join("\n", lines) + "\nEND PROBE679C");
}

[Trait("Probe", "MySql")]
public sealed class MySqlProbe679c : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _f;

    public MySqlProbe679c(MySqlFixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe()
    {
        List<string> log = new();
        using IDbConnection c = _f.OpenConnection();
        log.Add(Probe679c.Q(c, "version", "SELECT VERSION()"));
        Probe679c.Each(
            c, log, "SELECT user_id, COUNT(*) FROM orders GROUP BY user_id ORDER BY user_id",
            ("from_from.as", "DELETE FROM orders AS `x` FROM users `y` WHERE `x`.user_id = `y`.id AND `y`.id = 1"),
            ("from_from.bare", "DELETE FROM orders `x` FROM users `y` WHERE `x`.user_id = `y`.id AND `y`.id = 1"));
        Probe679c.Report("MySql", log);
    }
}

[Trait("Probe", "SqlServer")]
public sealed class SqlServerProbe679c : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _f;

    public SqlServerProbe679c(SqlServerFixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe()
    {
        List<string> log = new();
        using IDbConnection c = _f.OpenConnection();
        log.Add(Probe679c.Q(c, "version", "SELECT SERVERPROPERTY('ProductVersion')"));
        Probe679c.Each(
            c, log, "SELECT user_id, COUNT(*) FROM orders GROUP BY user_id ORDER BY user_id",
            ("from_from.as", "DELETE FROM orders AS \"x\" FROM users \"y\" WHERE \"x\".user_id = \"y\".id AND \"y\".id = 1"),
            ("from_from.bare", "DELETE FROM orders \"x\" FROM users \"y\" WHERE \"x\".user_id = \"y\".id AND \"y\".id = 1"),
            ("from_from.unaliased", "DELETE FROM orders FROM users \"y\" WHERE orders.user_id = \"y\".id AND \"y\".id = 1"));
        Probe679c.Report("SqlServer", log);
    }
}

public abstract class OracleProbe679cBase
{
    protected static List<string> Collect(IDbConnection c)
    {
        List<string> log = new();
        log.Add(Probe679c.Q(c, "version", "SELECT banner_full FROM v$version"));
        Probe679c.Each(
            c, log, "SELECT user_id, COUNT(*) FROM orders GROUP BY user_id ORDER BY user_id",
            ("from_from", "DELETE FROM orders \"x\" FROM users \"y\" WHERE \"x\".user_id = \"y\".id AND \"y\".id = 1"),
            ("from_from.join", "DELETE FROM orders \"x\" FROM users \"y\" INNER JOIN users \"z\" ON \"z\".id = \"y\".id WHERE \"x\".user_id = \"y\".id AND \"y\".id = 1"),
            ("relisted", "DELETE \"x\" FROM orders \"x\" INNER JOIN users \"y\" ON \"x\".user_id = \"y\".id WHERE \"y\".id = 1"),
            ("relisted_join", "DELETE \"x\" FROM users \"y\" INNER JOIN orders \"x\" ON \"x\".user_id = \"y\".id WHERE \"y\".id = 1"),
            ("using", "DELETE FROM orders \"x\" USING users \"y\" WHERE \"x\".user_id = \"y\".id AND \"y\".id = 1"));
        foreach ((string label, string sql) in new[]
        {
            ("ret.from_from", "DELETE FROM orders \"x\" FROM users \"y\" WHERE \"x\".user_id = \"y\".id AND \"y\".id = 2 RETURNING \"x\".id INTO :rid"),
            ("ret.using", "DELETE FROM orders \"x\" USING users \"y\" WHERE \"x\".user_id = \"y\".id AND \"y\".id = 2 RETURNING \"x\".id INTO :rid"),
            ("ret.update_from", "UPDATE orders \"x\" SET amount = 7 FROM users \"y\" WHERE \"x\".user_id = \"y\".id AND \"y\".id = 2 RETURNING \"x\".id INTO :rid"),
        })
        {
            using IDbTransaction tx = c.BeginTransaction();
            DynamicParameters p = new();
            p.Add("rid", dbType: DbType.Int32, direction: ParameterDirection.Output);
            try
            {
                int n = c.Execute(sql, p, tx);
                log.Add($"{label}: ok, {n} row(s), rid=[{p.Get<object>("rid")}]");
            }
            catch (Exception e)
            {
                log.Add($"{label}: ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}");
            }

            tx.Rollback();
        }

        return log;
    }
}

[Trait("Probe", "Oracle")]
public sealed class OracleProbe679c : OracleProbe679cBase, IClassFixture<OracleFixture>
{
    private readonly OracleFixture _f;

    public OracleProbe679c(OracleFixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe679c.Report("Oracle", Collect(c));
    }
}

[Trait("Probe", "Oracle23ai")]
public sealed class Oracle23aiProbe679c : OracleProbe679cBase, IClassFixture<Oracle23aiFixture>
{
    private readonly Oracle23aiFixture _f;

    public Oracle23aiProbe679c(Oracle23aiFixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe679c.Report("Oracle23ai", Collect(c));
    }
}
