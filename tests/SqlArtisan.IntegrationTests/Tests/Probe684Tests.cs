using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

// Throwaway observation probe for #684 (never merged): each lane records what its
// engine does and fails on purpose so the log carries the record.
internal static class Probe684
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

    // Builds through SqlArtisan for the connection's dialect, as a user's call would run.
    public static string S(IDbConnection c, string label, Func<ISqlBuilder> query)
    {
        ISqlBuilder builder;
        try
        {
            builder = query();
        }
        catch (Exception e)
        {
            return $"{label}: FACTORY {e.GetType().Name}: {e.Message.Split('\n')[0]}";
        }

        try
        {
            object? v = c.ExecuteScalar(builder);
            return $"{label}: {(v is null ? "NULL" : $"[{v}]")}";
        }
        catch (Exception e)
        {
            return $"{label}: ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}";
        }
    }

    public static string T(string label, ISqlBuilder query, Dbms dbms)
    {
        try
        {
            return $"{label}: TEXT {query.Build(dbms).Text}";
        }
        catch (Exception e)
        {
            return $"{label}: BUILD {e.GetType().Name}: {e.Message.Split('\n')[0]}";
        }
    }

    public static void Report(string engine, IEnumerable<string> lines) =>
        Assert.True(false, $"PROBE684 {engine}\n" + string.Join("\n", lines) + "\nEND PROBE684");

    public static readonly string[] TemporalTexts =
    [
        "2020-03-15 10:30:00", "2020-03-15 10:30", "2020-3-15 10:30:00",
        "2020-03-15 10:30:00.000", "20200315103000", "2020-03-15", "15-MAR-20", "abc",
    ];

    // Every seeded user has created_at 2020-03-15 10:30:00, so a match counts 5.
    public static IEnumerable<string> TemporalText(IDbConnection c)
    {
        UsersTable u = new();
        foreach (string s in TemporalTexts)
        {
            yield return S(c, $"temporal_text.eq.'{s}'", () =>
                Select(Count(u.Id)).From(u).Where(u.CreatedAt == s));
        }

        foreach (string s in new[] { "2020-03-15", "2020-03-16", "abc" })
        {
            yield return S(c, $"temporal_text.ge.'{s}'", () =>
                Select(Count(u.Id)).From(u).Where(u.CreatedAt >= s));
        }
    }

    public static readonly string[] GroupKeys = ["1", "2", "2.5", "2.0", "0", "-1", "'x'"];

    public static IEnumerable<string> GroupBy(IDbConnection c)
    {
        foreach (string k in GroupKeys)
        {
            yield return Q(c, $"group_by.aggregate_only.{k}", $"SELECT COUNT(*) FROM users GROUP BY {k}");
        }

        yield return Q(
            c, "group_by.grouped_plus_1",
            "SELECT department_id, COUNT(*) FROM users GROUP BY department_id, 1 ORDER BY department_id");
        yield return Q(
            c, "group_by.select_list_1",
            "SELECT department_id, COUNT(*) FROM users GROUP BY 1");
        UsersTable u = new();
        yield return S(c, "group_by.sqlartisan.1", () => Select(Count(u.Id)).From(u).GroupBy(1));
        yield return S(c, "group_by.sqlartisan.2.5", () => Select(Count(u.Id)).From(u).GroupBy(2.5));
    }
}

[Trait("Probe", "MySql")]
public sealed class MySqlProbe684 : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _f;

    public MySqlProbe684(MySqlFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        List<string> log = new();
        UsersTable u = new();
        using (IDbConnection c = _f.OpenConnection())
        {
            log.Add(Probe684.Q(c, "version", "SELECT VERSION(), @@sql_mode, @@character_set_connection"));

            DateTimePart first = Enum.GetValues<DateTimePart>()[0];
            log.Add(Probe684.T(
                "timestampadd.text", Select(Timestampadd(first, 1, u.CreatedAt)).From(u), Dbms.MySql));
            log.Add(Probe684.T(
                "interval.text", Select(DateAdd(u.CreatedAt, Interval(1, first))).From(u), Dbms.MySql));
            foreach (DateTimePart p in Enum.GetValues<DateTimePart>())
            {
                log.Add(Probe684.S(c, $"timestampadd.{p}", () =>
                    Select(Timestampadd(p, 1, u.CreatedAt)).From(u).Where(u.Id == 1)));
                log.Add(Probe684.S(c, $"timestampdiff.{p}", () =>
                    Select(Timestampdiff(p, u.CreatedAt, u.CreatedAt)).From(u).Where(u.Id == 1)));
                log.Add(Probe684.S(c, $"interval.{p}", () =>
                    Select(DateAdd(u.CreatedAt, Interval(1, p))).From(u).Where(u.Id == 1)));
                log.Add(Probe684.S(c, $"extract.{p}", () =>
                    Select(Extract(p, u.CreatedAt)).From(u).Where(u.Id == 1)));
            }

            log.AddRange(Probe684.TemporalText(c));
            log.AddRange(Probe684.GroupBy(c));

            log.Add(Probe684.X(c, "binary.create", "CREATE TABLE pb684 (id INT, b VARBINARY(10))"));
            log.Add(Probe684.X(c, "binary.insert", "INSERT INTO pb684 VALUES (1, 'abc'), (2, 'ABC')"));
            foreach (string s in new[] { "abc", "ABC", "abc ", "616263" })
            {
                log.Add(Probe684.Q(
                    c, $"binary_text.eq.'{s}'", "SELECT id FROM pb684 WHERE b = @s ORDER BY id", new { s }));
            }

            log.Add(Probe684.X(c, "binary.drop", "DROP TABLE pb684"));

            // A BEFORE INSERT trigger filling an omitted NOT NULL column (default strict mode).
            log.Add(Probe684.X(
                c, "trigger.create_table", "CREATE TABLE pt684 (id INT PRIMARY KEY, code VARCHAR(10) NOT NULL)"));
            log.Add(Probe684.X(
                c, "trigger.create",
                "CREATE TRIGGER pt684_bi BEFORE INSERT ON pt684 FOR EACH ROW "
                + "SET NEW.code = COALESCE(NEW.code, 'auto')"));
            log.Add(Probe684.X(c, "trigger.insert_omitted", "INSERT INTO pt684 (id) VALUES (1)"));
            log.Add(Probe684.X(c, "trigger.insert_null", "INSERT INTO pt684 (id, code) VALUES (2, NULL)"));
            log.Add(Probe684.Q(c, "trigger.rows", "SELECT id, code FROM pt684 ORDER BY id"));
            log.Add(Probe684.X(
                c, "trigger.control_table", "CREATE TABLE pc684 (id INT PRIMARY KEY, code VARCHAR(10) NOT NULL)"));
            log.Add(Probe684.X(c, "trigger.control_insert_omitted", "INSERT INTO pc684 (id) VALUES (1)"));
            log.Add(Probe684.X(c, "trigger.drop", "DROP TABLE pt684"));
            log.Add(Probe684.X(c, "trigger.control_drop", "DROP TABLE pc684"));

            foreach (int n in new[] { 64, 65, 256, 257 })
            {
                string a = new('a', n);
                log.Add(Probe684.Q(c, $"ident.alias.ascii.{n}", $"SELECT 1 AS `{a}`"));
            }

            foreach (int n in new[] { 64, 65, 256, 257 })
            {
                string a = new('あ', n);
                log.Add(Probe684.Q(c, $"ident.alias.kana.{n}", $"SELECT 1 AS `{a}`"));
            }

            foreach (int n in new[] { 64, 65 })
            {
                string t = new('t', n);
                log.Add(Probe684.X(c, $"ident.table.{n}", $"CREATE TABLE `{t}` (x INT)"));
                log.Add(Probe684.X(c, $"ident.table.{n}.drop", $"DROP TABLE IF EXISTS `{t}`"));
                string col = new('c', n);
                log.Add(Probe684.Q(c, $"ident.column_ref.{n}", $"SELECT `{col}` FROM users"));
            }
        }

        // Zero dates: written outside strict mode, read under both modes.
        using (IDbConnection c = _f.OpenConnection())
        {
            log.Add(Probe684.X(c, "zero.mode", "SET SESSION sql_mode = ''"));
            log.Add(Probe684.X(
                c, "zero.create", "CREATE TABLE pz684 (id INT, d DATE NOT NULL, dt DATETIME NOT NULL)"));
            log.Add(Probe684.X(
                c, "zero.insert",
                "INSERT INTO pz684 VALUES (1, '0000-00-00', '0000-00-00 00:00:00'), "
                + "(2, '2024-01-01', '2024-01-01 00:00:00')"));
            log.AddRange(ZeroDateReads(c, "nonstrict"));
        }

        using (IDbConnection c = _f.OpenConnection())
        {
            log.Add(Probe684.Q(c, "zero.default_mode", "SELECT @@sql_mode"));
            log.Add(Probe684.X(
                c, "zero.strict_insert", "INSERT INTO pz684 VALUES (3, '0000-00-00', '0000-00-00 00:00:00')"));
            log.AddRange(ZeroDateReads(c, "strict"));
            log.Add(Probe684.X(c, "zero.drop", "DROP TABLE pz684"));
        }

        log.AddRange(Rerun(_f.ConnectionString));
        Probe684.Report("MySql", log);
    }

    // Second run: the alias column name each engine returns, and the trigger as root.
    private static IEnumerable<string> Rerun(string connectionString)
    {
        List<string> log = new();
        using (IDbConnection c = new MySqlConnector.MySqlConnection(connectionString))
        {
            c.Open();
            foreach (int n in new[] { 64, 256, 257, 300, 1000 })
            {
                foreach (char ch in new[] { 'a', 'あ' })
                {
                    string label = $"alias_name.{(ch == 'a' ? "ascii" : "kana")}.{n}";
                    try
                    {
                        IDictionary<string, object> row =
                            (IDictionary<string, object>)c.Query($"SELECT 1 AS `{new string(ch, n)}`").First();
                        string key = row.Keys.Single();
                        log.Add($"{label}: returned name length {key.Length}, same={key == new string(ch, n)}");
                    }
                    catch (Exception e)
                    {
                        log.Add($"{label}: ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}");
                    }

                    log.Add(Probe684.Q(c, label + ".warnings", "SHOW WARNINGS"));
                }
            }
        }

        MySqlConnector.MySqlConnectionStringBuilder root = new(connectionString) { UserID = "root" };
        using (IDbConnection c = new MySqlConnector.MySqlConnection(root.ConnectionString))
        {
            try
            {
                c.Open();
                log.Add("root.open: ok");
            }
            catch (Exception e)
            {
                log.Add($"root.open: ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}");
                return log;
            }

            log.Add(Probe684.Q(c, "root.mode", "SELECT @@sql_mode, DATABASE()"));
            log.Add(Probe684.X(
                c, "trigger2.create_table", "CREATE TABLE pt684 (id INT PRIMARY KEY, code VARCHAR(10) NOT NULL)"));
            log.Add(Probe684.X(
                c, "trigger2.create",
                "CREATE TRIGGER pt684_bi BEFORE INSERT ON pt684 FOR EACH ROW "
                + "SET NEW.code = COALESCE(NEW.code, 'auto')"));
            log.Add(Probe684.X(c, "trigger2.insert_omitted", "INSERT INTO pt684 (id) VALUES (1)"));
            log.Add(Probe684.X(c, "trigger2.insert_null", "INSERT INTO pt684 (id, code) VALUES (2, NULL)"));
            log.Add(Probe684.Q(c, "trigger2.rows", "SELECT id, code FROM pt684 ORDER BY id"));
            log.Add(Probe684.X(c, "trigger2.drop", "DROP TABLE pt684"));
        }

        return log;
    }

    private static IEnumerable<string> ZeroDateReads(IDbConnection c, string mode)
    {
        string[] predicates =
        [
            "d IS NULL", "dt IS NULL", "d IS NOT NULL", "d IS NULL AND id > 0", "(d IS NULL OR id = 99)",
            "NOT (d IS NULL)",
        ];
        foreach (string w in predicates)
        {
            yield return Probe684.Q(c, $"zero.{mode}.where[{w}]", $"SELECT id FROM pz684 WHERE {w} ORDER BY id");
        }

        yield return Probe684.Q(c, $"zero.{mode}.select_list", "SELECT id, d IS NULL FROM pz684 ORDER BY id");
        yield return Probe684.Q(
            c, $"zero.{mode}.having",
            "SELECT d, COUNT(*) FROM pz684 GROUP BY d HAVING d IS NULL");
        yield return Probe684.Q(
            c, $"zero.{mode}.on",
            "SELECT a.id FROM pz684 a JOIN pz684 b ON a.id = b.id AND a.d IS NULL ORDER BY a.id");
    }
}

public abstract class OracleProbe684Base
{
    protected static List<string> Collect(IDbConnection c)
    {
        const string D = " FROM DUAL";
        List<string> log = new();
        log.Add(Probe684.Q(c, "version", "SELECT banner_full FROM v$version"));
        log.Add(Probe684.Q(
            c, "nls",
            "SELECT parameter, value FROM nls_database_parameters "
            + "WHERE parameter IN ('NLS_CHARACTERSET', 'NLS_DATE_FORMAT', 'NLS_LANGUAGE')"));
        log.Add(Probe684.Q(
            c, "nls.session", "SELECT parameter, value FROM nls_session_parameters WHERE parameter = 'NLS_DATE_FORMAT'"));

        log.AddRange(Probe684.GroupBy(c));
        log.Add(Probe684.Q(
            c, "group_by_position.param", "SELECT value FROM v$parameter WHERE name = 'group_by_position_enabled'"));
        log.Add(Probe684.X(c, "group_by_position.enable", "ALTER SESSION SET group_by_position_enabled = TRUE"));
        log.Add(Probe684.Q(
            c, "group_by_position.select_list_1", "SELECT department_id, COUNT(*) FROM users GROUP BY 1"));
        log.Add(Probe684.Q(c, "group_by_position.aggregate_only.1", "SELECT COUNT(*) FROM users GROUP BY 1"));
        log.Add(Probe684.X(c, "group_by_position.disable", "ALTER SESSION SET group_by_position_enabled = FALSE"));

        log.AddRange(Probe684.TemporalText(c));

        log.Add(Probe684.X(c, "binary.create", "CREATE TABLE pb684 (id NUMBER, b RAW(10))"));
        log.Add(Probe684.X(c, "binary.insert1", "INSERT INTO pb684 VALUES (1, HEXTORAW('616263'))"));
        log.Add(Probe684.X(c, "binary.insert2", "INSERT INTO pb684 VALUES (2, HEXTORAW('414243'))"));
        foreach (string s in new[] { "abc", "ABC", "616263" })
        {
            log.Add(Probe684.Q(
                c, $"binary_text.eq.'{s}'", "SELECT id FROM pb684 WHERE b = :s ORDER BY id", new { s }));
        }

        log.Add(Probe684.X(c, "binary.drop", "DROP TABLE pb684"));

        log.Add(Probe684.X(
            c, "trigger.create_table", "CREATE TABLE pt684 (id NUMBER PRIMARY KEY, code VARCHAR2(10) NOT NULL)"));
        log.Add(Probe684.X(
            c, "trigger.create",
            "CREATE OR REPLACE TRIGGER pt684_bi BEFORE INSERT ON pt684 FOR EACH ROW "
            + "BEGIN IF :NEW.code IS NULL THEN :NEW.code := 'auto'; END IF; END;"));
        log.Add(Probe684.Q(
            c, "trigger.status", "SELECT status FROM user_triggers WHERE trigger_name = 'PT684_BI'"));
        log.Add(Probe684.X(c, "trigger.insert_omitted", "INSERT INTO pt684 (id) VALUES (1)"));
        log.Add(Probe684.X(c, "trigger.insert_null", "INSERT INTO pt684 (id, code) VALUES (2, NULL)"));
        log.Add(Probe684.Q(c, "trigger.rows", "SELECT id, code FROM pt684 ORDER BY id"));
        log.Add(Probe684.X(c, "trigger.drop", "DROP TABLE pt684"));

        foreach (int n in new[] { 128, 129 })
        {
            log.Add(Probe684.Q(c, $"ident.alias.ascii.{n}", $"SELECT 1 AS \"{new string('a', n)}\"" + D));
            log.Add(Probe684.X(c, $"ident.table.{n}", $"CREATE TABLE \"{new string('t', n)}\" (x NUMBER)"));
            log.Add(Probe684.X(c, $"ident.table.{n}.drop", $"DROP TABLE \"{new string('t', n)}\""));
        }

        foreach (int n in new[] { 42, 43, 128 })
        {
            log.Add(Probe684.Q(c, $"ident.alias.kana.{n}", $"SELECT 1 AS \"{new string('あ', n)}\"" + D));
        }

        log.AddRange(BindNames((OracleConnection)c));
        return log;
    }

    private static IEnumerable<string> BindNames(OracleConnection c)
    {
        foreach (int n in new[] { 30, 31, 128, 129 })
        {
            string name = new('b', n);
            string label = $"bind_name.select.{n}";
            try
            {
                using OracleCommand cmd = c.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = $"SELECT :{name} FROM DUAL";
                cmd.Parameters.Add(new OracleParameter(name, 1));
                label += $": [{cmd.ExecuteScalar()}]";
            }
            catch (Exception e)
            {
                label += $": ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}";
            }

            yield return label;
        }

        foreach (int n in new[] { 30, 31, 128, 129 })
        {
            string name = new('r', n);
            string label = $"bind_name.returning_into.{n}";
            try
            {
                using OracleCommand cmd = c.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = $"UPDATE users SET age = age WHERE id = 1 RETURNING id INTO :{name}";
                OracleParameter p = new(name, OracleDbType.Int32) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(p);
                int rows = cmd.ExecuteNonQuery();
                label += $": ok, {rows} row(s), out [{p.Value}]";
            }
            catch (Exception e)
            {
                label += $": ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}";
            }

            yield return label;
        }
    }
}

[Trait("Probe", "Oracle")]
public sealed class OracleProbe684 : OracleProbe684Base, IClassFixture<OracleFixture>
{
    private readonly OracleFixture _f;

    public OracleProbe684(OracleFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe684.Report("Oracle", Collect(c));
    }
}

[Trait("Probe", "Oracle23ai")]
public sealed class Oracle23aiProbe684 : OracleProbe684Base, IClassFixture<Oracle23aiFixture>
{
    private readonly Oracle23aiFixture _f;

    public Oracle23aiProbe684(Oracle23aiFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe684.Report("Oracle23ai", Collect(c));
    }
}

public abstract class SqlServerProbe684Base
{
    protected static List<string> Collect(IDbConnection c)
    {
        List<string> log = new();
        UsersTable u = new();
        log.Add(Probe684.Q(c, "version", "SELECT @@VERSION, SERVERPROPERTY('ProductVersion'), @@LANGUAGE"));
        log.Add(Probe684.Q(c, "dateformat", "SELECT date_format FROM sys.dm_exec_sessions WHERE session_id = @@SPID"));

        DateTimePart first = Enum.GetValues<DateTimePart>()[0];
        log.Add(Probe684.T(
            "dateadd.text", Select(Dateadd(first, 1, u.CreatedAt)).From(u), Dbms.SqlServer));
        foreach (DateTimePart p in Enum.GetValues<DateTimePart>())
        {
            log.Add(Probe684.S(c, $"dateadd.{p}", () =>
                Select(Dateadd(p, 1, u.CreatedAt)).From(u).Where(u.Id == 1)));
            log.Add(Probe684.S(c, $"datediff.{p}", () =>
                Select(Datediff(p, u.CreatedAt, u.CreatedAt)).From(u).Where(u.Id == 1)));
            log.Add(Probe684.S(c, $"datepart.{p}", () =>
                Select(Datepart(p, u.CreatedAt)).From(u).Where(u.Id == 1)));
            log.Add(Probe684.S(c, $"datetrunc.{p}", () =>
                Select(Datetrunc(p, u.CreatedAt)).From(u).Where(u.Id == 1)));
        }

        log.AddRange(Probe684.GroupBy(c));
        log.AddRange(Probe684.TemporalText(c));

        log.Add(Probe684.X(c, "binary.create", "CREATE TABLE pb684 (id INT, b VARBINARY(10))"));
        log.Add(Probe684.X(c, "binary.insert", "INSERT INTO pb684 VALUES (1, 0x616263), (2, 0x414243)"));
        foreach (string s in new[] { "abc", "ABC", "616263" })
        {
            log.Add(Probe684.Q(
                c, $"binary_text.eq.'{s}'", "SELECT id FROM pb684 WHERE b = @s ORDER BY id", new { s }));
        }

        log.Add(Probe684.Q(c, "binary_text.varchar_literal", "SELECT id FROM pb684 WHERE b = 'abc' ORDER BY id"));
        log.Add(Probe684.X(c, "binary.drop", "DROP TABLE pb684"));

        log.Add(Probe684.X(
            c, "trigger.create_table", "CREATE TABLE pt684 (id INT PRIMARY KEY, code NVARCHAR(10) NOT NULL)"));
        log.Add(Probe684.X(
            c, "trigger.create",
            "CREATE TRIGGER pt684_ioi ON pt684 INSTEAD OF INSERT AS "
            + "INSERT INTO pt684 (id, code) SELECT id, COALESCE(code, N'auto') FROM inserted"));
        log.Add(Probe684.X(c, "trigger.insert_omitted", "INSERT INTO pt684 (id) VALUES (1)"));
        log.Add(Probe684.X(c, "trigger.insert_null", "INSERT INTO pt684 (id, code) VALUES (2, NULL)"));
        log.Add(Probe684.Q(c, "trigger.rows", "SELECT id, code FROM pt684 ORDER BY id"));
        log.Add(Probe684.X(c, "trigger.drop", "DROP TABLE pt684"));
        log.Add(Probe684.X(
            c, "trigger_after.create_table", "CREATE TABLE pa684 (id INT PRIMARY KEY, code NVARCHAR(10) NOT NULL)"));
        log.Add(Probe684.X(
            c, "trigger_after.create",
            "CREATE TRIGGER pa684_ai ON pa684 AFTER INSERT AS UPDATE pa684 SET code = N'auto' WHERE code IS NULL"));
        log.Add(Probe684.X(c, "trigger_after.insert_omitted", "INSERT INTO pa684 (id) VALUES (1)"));
        log.Add(Probe684.X(c, "trigger_after.drop", "DROP TABLE pa684"));

        foreach (int n in new[] { 128, 129 })
        {
            log.Add(Probe684.Q(c, $"ident.alias.ascii.{n}", $"SELECT 1 AS [{new string('a', n)}]"));
            log.Add(Probe684.Q(c, $"ident.alias.kana.{n}", $"SELECT 1 AS [{new string('あ', n)}]"));
            log.Add(Probe684.X(c, $"ident.table.{n}", $"CREATE TABLE [{new string('t', n)}] (x INT)"));
            log.Add(Probe684.X(c, $"ident.table.{n}.drop", $"DROP TABLE IF EXISTS [{new string('t', n)}]"));
        }

        return log;
    }
}

[Trait("Probe", "SqlServer")]
public sealed class SqlServerProbe684 : SqlServerProbe684Base, IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _f;

    public SqlServerProbe684(SqlServerFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe684.Report("SqlServer", Collect(c));
    }
}

[Trait("Probe", "SqlServer2025")]
public sealed class SqlServer2025Probe684 : SqlServerProbe684Base, IClassFixture<SqlServer2025Fixture>
{
    private readonly SqlServer2025Fixture _f;

    public SqlServer2025Probe684(SqlServer2025Fixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe684.Report("SqlServer2025", Collect(c));
    }
}

[Trait("Probe", "PostgreSql")]
public sealed class PostgreSqlProbe684 : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _f;

    public PostgreSqlProbe684(PostgreSqlFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        List<string> log = new() { Probe684.Q(c, "version", "SHOW server_version") };
        log.AddRange(Probe684.TemporalText(c));
        log.AddRange(Probe684.GroupBy(c));
        Probe684.Report("PostgreSql", log);
    }
}

[Trait("Probe", "Sqlite")]
public sealed class SqliteProbe684 : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _f;

    public SqliteProbe684(SqliteFixture f) => _f = f;

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        List<string> log = new() { Probe684.Q(c, "version", "SELECT sqlite_version()") };
        log.AddRange(Probe684.TemporalText(c));
        log.AddRange(Probe684.GroupBy(c));
        Probe684.Report("Sqlite", log);
    }
}
