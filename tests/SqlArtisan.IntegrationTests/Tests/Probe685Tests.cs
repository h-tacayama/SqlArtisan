using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

// Throwaway observation probe for #685 (never merged): each lane records what its
// engine does and fails on purpose so the log carries the record.
internal static class Probe
{
    public static string Q(IDbConnection c, string label, string sql, object? p = null)
    {
        try
        {
            IEnumerable<string> rows = c.Query(sql, p).Select(r => string.Join(
                ",",
                ((IDictionary<string, object>)r).Values.Select(v => v is null ? "NULL" : $"[{v}]")));
            return $"{label}: {string.Join(" | ", rows)}";
        }
        catch (Exception e)
        {
            return $"{label}: ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}";
        }
    }

    public static string X(IDbConnection c, string label, string sql, object? p = null)
    {
        try
        {
            return $"{label}: ok, {c.Execute(sql, p)} row(s)";
        }
        catch (Exception e)
        {
            return $"{label}: ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}";
        }
    }

    public static void Report(string engine, IEnumerable<string> lines) =>
        Assert.True(false, $"PROBE685 {engine}\n" + string.Join("\n", lines) + "\nEND PROBE685");

    public static IEnumerable<string> Common(IDbConnection c, string dual, string except)
    {
        yield return Q(c, "setop.union_intersect",
            $"SELECT 1 AS x{dual} UNION SELECT 2{dual} INTERSECT SELECT 2{dual}");
        yield return Q(c, "setop.except_intersect",
            $"SELECT 1 AS x{dual} {except} SELECT 1{dual} INTERSECT SELECT 2{dual}");
        yield return Q(c, "setop.intersect_union",
            $"SELECT 2 AS x{dual} INTERSECT SELECT 2{dual} UNION SELECT 1{dual}");
    }
}

[Trait("Probe", "MySql")]
public sealed class MySqlProbe685 : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _f;

    public MySqlProbe685(MySqlFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        List<string> log = new();
        using (IDbConnection c = _f.OpenConnection())
        {
            log.Add(Probe.Q(c, "version", "SELECT VERSION(), @@sql_mode"));
            log.Add(Probe.Q(c, "regexp_replace.3", "SELECT REGEXP_REPLACE('xooxo', 'o', '0')"));
            log.Add(Probe.Q(c, "regexp_replace.4", "SELECT REGEXP_REPLACE('xooxo', 'o', '0', 1)"));
            log.Add(Probe.Q(
                c, "length.param", "SELECT LENGTH(@s), CHAR_LENGTH(@s)", new { s = "é" }));
            log.Add(Probe.Q(c, "greatest_least.null", "SELECT GREATEST(NULL, 3, 1), LEAST(NULL, 3, 1)"));
            log.AddRange(Probe.Common(c, "", "EXCEPT"));
            log.Add(Probe.Q(
                c, "like.backslash.same", "SELECT CASE WHEN @v LIKE @p THEN 1 ELSE 0 END",
                new { v = @"a\b", p = @"a\b" }));
            log.Add(Probe.Q(
                c, "like.backslash.vs_ab", "SELECT CASE WHEN @v LIKE @p THEN 1 ELSE 0 END",
                new { v = "ab", p = @"a\b" }));
            log.Add(Probe.X(c, "json.create", "CREATE TEMPORARY TABLE pj (id INT, data JSON)"));
            log.Add(Probe.X(
                c, "json.insert",
                "INSERT INTO pj VALUES (1, '{\"a\": 1, \"address\": \"x\"}'), (2, '[10, 20, 30]')"));
            log.Add(Probe.Q(c, "json.arrow_key", "SELECT id, (data -> 'address') FROM pj ORDER BY id"));
            log.Add(Probe.Q(c, "json.arrow_path", "SELECT id, (data -> '$.address') FROM pj ORDER BY id"));
            log.Add(Probe.Q(c, "json.arrow_int", "SELECT id, (data -> 0) FROM pj ORDER BY id"));
            log.Add(Probe.Q(c, "json.arrow_index_path", "SELECT id, (data -> '$[0]') FROM pj ORDER BY id"));
            log.Add(Probe.Q(c, "json.arrowtext_key", "SELECT id, (data ->> 'a') FROM pj ORDER BY id"));
            log.Add(Probe.Q(c, "json.arrowtext_path", "SELECT id, (data ->> '$.a') FROM pj ORDER BY id"));
            log.Add(Probe.X(
                c, "er1093.delete_reselect",
                "DELETE FROM users WHERE id IN (SELECT `i`.id FROM users `i` WHERE `i`.name = @n)",
                new { n = "no such name" }));
            log.Add(Probe.X(
                c, "er1093.update_reselect",
                "UPDATE users SET age = age WHERE id IN (SELECT `i`.id FROM users `i` WHERE `i`.name = @n)",
                new { n = "no such name" }));
            log.Add(Probe.X(
                c, "er1093.delete_correlated_other_table",
                "DELETE FROM users AS `u` WHERE `u`.id < 0 AND NOT EXISTS "
                + "(SELECT `o`.id FROM orders `o` WHERE `o`.user_id = `u`.id)"));
            log.Add(Probe.Q(c, "fulljoin.emitted", "SELECT id FROM orders FULL JOIN users ON user_id = id"));
            log.Add(Probe.Q(
                c, "fulljoin.qualified_count",
                "SELECT COUNT(*) FROM orders FULL JOIN users ON user_id = users.id"));
            log.Add(Probe.Q(
                c, "fulljoin.inner_count",
                "SELECT COUNT(*) FROM orders INNER JOIN users ON orders.user_id = users.id"));
            log.Add(Probe.Q(
                c, "fulljoin.counts",
                "SELECT (SELECT COUNT(*) FROM orders), (SELECT COUNT(*) FROM users)"));
            log.Add(Probe.Q(
                c, "zip.numeric_only", "SELECT COUNT(*) FROM (SELECT '01500001' AS z) t WHERE z = 1500001"));
            log.Add(Probe.Q(
                c, "zip.with_text",
                "SELECT COUNT(*) FROM (SELECT '01500001' AS z UNION ALL SELECT 'abc') t WHERE z = 1500001"));
            log.Add(Probe.Q(c, "group_by_2.0", "SELECT COUNT(*) FROM users GROUP BY 2.0"));
            log.Add(Probe.Q(
                c, "limit_in_not_in",
                "SELECT id FROM users WHERE id NOT IN (SELECT user_id FROM orders ORDER BY user_id LIMIT 2)"));
        }

        using (IDbConnection c = _f.OpenConnection())
        {
            log.Add(Probe.X(
                c, "nbe.set", "SET SESSION sql_mode = CONCAT(@@sql_mode, ',NO_BACKSLASH_ESCAPES')"));
            log.Add(Probe.Q(c, "nbe.literal", @"SELECT '\\' AS lit, LENGTH('\\') AS len"));
            log.Add(Probe.Q(
                c, "nbe.escape_emitted", @"SELECT id FROM users WHERE name LIKE @p ESCAPE '\\'",
                new { p = "a" }));
        }

        Probe.Report("MySql", log);
    }
}

public abstract class OracleProbe685Base
{
    protected static List<string> Collect(IDbConnection c)
    {
        const string D = " FROM DUAL";
        List<string> log = new();
        log.Add(Probe.Q(c, "version", "SELECT banner_full FROM v$version"));
        log.Add(Probe.Q(c, "regexp_replace.3", "SELECT REGEXP_REPLACE('xooxo', 'o', '0')" + D));
        log.Add(Probe.Q(c, "regexp_replace.4", "SELECT REGEXP_REPLACE('xooxo', 'o', '0', 1)" + D));
        log.Add(Probe.Q(
            c, "length.param", "SELECT LENGTH(:s), LENGTHB(:s)" + D, new { s = "é" }));
        log.Add(Probe.Q(c, "greatest_least.null", "SELECT GREATEST(NULL, 3, 1), LEAST(NULL, 3, 1)" + D));
        log.AddRange(Probe.Common(c, D, "MINUS"));
        log.AddRange(Probe.Common(c, D, "EXCEPT").Select(s => "except-spelling " + s));
        log.Add(Probe.Q(
            c, "like.backslash.same", "SELECT CASE WHEN :v LIKE :p THEN 1 ELSE 0 END" + D,
            new { v = @"a\b", p = @"a\b" }));
        log.Add(Probe.Q(
            c, "like.backslash.vs_ab", "SELECT CASE WHEN :v LIKE :p THEN 1 ELSE 0 END" + D,
            new { v = "ab", p = @"a\b" }));
        log.Add(Probe.Q(
            c, "zip.numeric_only",
            "SELECT COUNT(*) FROM (SELECT '01500001' z" + D + ") t WHERE z = 1500001"));
        log.Add(Probe.Q(
            c, "zip.with_text",
            "SELECT COUNT(*) FROM (SELECT '01500001' z" + D + " UNION ALL SELECT 'abc'" + D
            + ") t WHERE z = 1500001"));
        log.Add(Probe.Q(c, "group_by_1.aggregate_only", "SELECT COUNT(*) FROM users GROUP BY 1"));
        log.Add(Probe.Q(c, "fromless.interval", "SELECT INTERVAL '123-11' YEAR(3) TO MONTH"));
        log.Add(Probe.X(c, "seq.create", "CREATE SEQUENCE probe685_seq"));
        log.Add(Probe.Q(c, "fromless.sequence", "SELECT probe685_seq.NEXTVAL, probe685_seq.CURRVAL"));
        log.Add(Probe.Q(c, "dual.sequence", "SELECT probe685_seq.NEXTVAL, probe685_seq.CURRVAL" + D));
        log.Add(Probe.X(c, "seq.drop", "DROP SEQUENCE probe685_seq"));
        log.AddRange(BindByName((OracleConnection)c));
        return log;
    }

    private static IEnumerable<string> BindByName(OracleConnection c)
    {
        foreach (bool byName in new[] { false, true })
        {
            foreach (int count in new[] { 1, 2 })
            {
                string label = $"bindbyname={byName}.params={count}";
                try
                {
                    using OracleCommand cmd = c.CreateCommand();
                    cmd.BindByName = byName;
                    cmd.CommandText = "SELECT :p0 + :p0 FROM DUAL";
                    cmd.Parameters.Add(new OracleParameter("p0", 1));
                    if (count == 2)
                    {
                        cmd.Parameters.Add(new OracleParameter("p0b", 10));
                    }

                    label += $": {cmd.ExecuteScalar()}";
                }
                catch (Exception e)
                {
                    label += $": ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}";
                }

                yield return label;
            }
        }
    }
}

[Trait("Probe", "Oracle")]
public sealed class OracleProbe685 : OracleProbe685Base, IClassFixture<OracleFixture>
{
    private readonly OracleFixture _f;

    public OracleProbe685(OracleFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe.Report("Oracle", Collect(c));
    }
}

[Trait("Probe", "Oracle23ai")]
public sealed class Oracle23aiProbe685 : OracleProbe685Base, IClassFixture<Oracle23aiFixture>
{
    private readonly Oracle23aiFixture _f;

    public Oracle23aiProbe685(Oracle23aiFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe.Report("Oracle23ai", Collect(c));
    }
}

public abstract class SqlServerProbe685Base
{
    protected static List<string> Collect(IDbConnection c)
    {
        List<string> log = new();
        log.Add(Probe.Q(c, "version", "SELECT @@VERSION"));
        log.Add(Probe.Q(c, "regexp_replace.3", "SELECT REGEXP_REPLACE('xooxo', 'o', '0')"));
        log.Add(Probe.Q(c, "regexp_replace.4", "SELECT REGEXP_REPLACE('xooxo', 'o', '0', 1)"));
        log.Add(Probe.Q(c, "greatest_least.null", "SELECT GREATEST(NULL, 3, 1), LEAST(NULL, 3, 1)"));
        log.AddRange(Probe.Common(c, "", "EXCEPT"));
        log.Add(Probe.Q(
            c, "like.backslash.same", "SELECT CASE WHEN @v LIKE @p THEN 1 ELSE 0 END",
            new { v = @"a\b", p = @"a\b" }));
        log.Add(Probe.Q(
            c, "like.backslash.vs_ab", "SELECT CASE WHEN @v LIKE @p THEN 1 ELSE 0 END",
            new { v = "ab", p = @"a\b" }));
        foreach (string type in new[] { "datetime2", "datetime2(7)", "datetime2(6)", "datetime2(3)", "datetime" })
        {
            log.Add(Probe.Q(
                c, $"datetrunc.microsecond.{type}",
                $"SELECT DATETRUNC(microsecond, CAST('2024-01-01 10:00:00.123' AS {type}))"));
        }

        log.Add(Probe.Q(
            c, "zip.numeric_only", "SELECT COUNT(*) FROM (SELECT '01500001' AS z) t WHERE z = 1500001"));
        log.Add(Probe.Q(
            c, "zip.with_text",
            "SELECT COUNT(*) FROM (SELECT '01500001' AS z UNION ALL SELECT 'abc') t WHERE z = 1500001"));
        log.Add(Probe.X(c, "output_star.delete", "DELETE FROM users OUTPUT * WHERE id < 0"));
        log.Add(Probe.X(c, "output_star.control", "DELETE FROM users OUTPUT DELETED.* WHERE id < 0"));
        return log;
    }
}

[Trait("Probe", "SqlServer")]
public sealed class SqlServerProbe685 : SqlServerProbe685Base, IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _f;

    public SqlServerProbe685(SqlServerFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe.Report("SqlServer", Collect(c));
    }
}

[Trait("Probe", "SqlServer2025")]
public sealed class SqlServer2025Probe685 : SqlServerProbe685Base, IClassFixture<SqlServer2025Fixture>
{
    private readonly SqlServer2025Fixture _f;

    public SqlServer2025Probe685(SqlServer2025Fixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe.Report("SqlServer2025", Collect(c));
    }
}

[Trait("Probe", "PostgreSql")]
public sealed class PostgreSqlProbe685 : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _f;

    public PostgreSqlProbe685(PostgreSqlFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        List<string> log = new() { Probe.Q(c, "version", "SHOW server_version") };
        log.AddRange(Probe.Common(c, "", "EXCEPT"));
        log.Add(Probe.Q(
            c, "like.backslash.same", "SELECT CASE WHEN @v LIKE @p THEN 1 ELSE 0 END",
            new { v = @"a\b", p = @"a\b" }));
        log.Add(Probe.Q(c, "greatest_least.null", "SELECT GREATEST(NULL, 3, 1), LEAST(NULL, 3, 1)"));
        Probe.Report("PostgreSql", log);
    }
}

[Trait("Probe", "Sqlite")]
public sealed class SqliteProbe685 : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _f;

    public SqliteProbe685(SqliteFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        List<string> log = new() { Probe.Q(c, "version", "SELECT sqlite_version()") };
        log.AddRange(Probe.Common(c, "", "EXCEPT"));
        log.Add(Probe.Q(
            c, "like.backslash.same", "SELECT CASE WHEN @v LIKE @p THEN 1 ELSE 0 END",
            new { v = @"a\b", p = @"a\b" }));
        Probe.Report("Sqlite", log);
    }
}
