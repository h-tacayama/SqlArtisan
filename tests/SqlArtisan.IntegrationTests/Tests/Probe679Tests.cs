using System.Data;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

// Throwaway observation probe for #679 (never merged): each lane records what its
// engine does and fails on purpose so the log carries the record.
internal static class Probe679
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

    // Runs the SQL SqlArtisan builds for the connection's dialect, as a user's call would.
    public static List<string> B(IDbConnection c, string label, Func<ISqlBuilder> query, Dbms dbms)
    {
        List<string> lines = new();
        try
        {
            lines.Add($"{label}.text: {query().Build(dbms).Text}");
        }
        catch (Exception e)
        {
            lines.Add($"{label}.text: BUILD {e.GetType().Name}: {e.Message.Split('\n')[0]}");
            return lines;
        }

        try
        {
            lines.Add($"{label}.run: ok, {c.Execute(query())} row(s)");
        }
        catch (Exception e)
        {
            lines.Add($"{label}.run: ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}");
        }

        return lines;
    }

    public static void Report(string engine, IEnumerable<string> lines) =>
        Assert.Fail($"PROBE679 {engine}\n" + string.Join("\n", lines) + "\nEND PROBE679");
}

internal sealed class Probe679Table : DbTableBase
{
    public Probe679Table(string name, string alias = "") : base(name, alias)
    {
        Id = new DbColumn(this, "id");
        Name = new DbColumn(this, "name");
    }

    public DbColumn Id { get; }

    public DbColumn Name { get; }
}

internal sealed class Probe679LabelTable : DbTableBase
{
    public Probe679LabelTable(string name, string alias = "") : base(name, alias)
    {
        Id = new DbColumn(this, "id");
        Label = new DbColumn(this, "label");
    }

    public DbColumn Id { get; }

    public DbColumn Label { get; }
}

[Trait("Probe", "MySql")]
public sealed class MySqlProbe679 : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _f;

    public MySqlProbe679(MySqlFixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe()
    {
        List<string> log = new();
        using IDbConnection c = _f.OpenConnection();
        log.Add(Probe679.Q(c, "version", "SELECT VERSION(), @@sql_mode"));

        // An INSERT ... SELECT reaching ON DUPLICATE KEY UPDATE through the IUpsert cast.
        foreach (string alias in new[] { "", "w" })
        {
            string tag = alias.Length == 0 ? "unaliased" : "aliased";
            log.Add(Probe679.X(c, $"upsert.{tag}.drop", "DROP TABLE IF EXISTS pt679, ps679"));
            log.Add(Probe679.X(c, $"upsert.{tag}.target", "CREATE TABLE pt679 (id INT PRIMARY KEY, name VARCHAR(20))"));
            log.Add(Probe679.X(c, $"upsert.{tag}.source", "CREATE TABLE ps679 (id INT, name VARCHAR(20))"));
            log.Add(Probe679.X(c, $"upsert.{tag}.seed_t", "INSERT INTO pt679 VALUES (1, 'old1'), (2, 'old2')"));
            log.Add(Probe679.X(c, $"upsert.{tag}.seed_s", "INSERT INTO ps679 VALUES (1, 'src1'), (3, 'src3')"));
            Probe679Table t = new("pt679");
            Probe679Table s = new("ps679", alias);
            log.AddRange(Probe679.B(c, $"upsert.{tag}", () =>
                ((IUpsert)InsertInto(t, t.Id, t.Name).Select(s.Id, s.Name).From(s))
                    .OnDuplicateKeyUpdate(t.Name == Excluded(t.Name)), Dbms.MySql));
            log.Add(Probe679.Q(c, $"upsert.{tag}.rows", "SELECT id, name FROM pt679 ORDER BY id"));
        }

        // A select list feeding the target's name from another source column.
        foreach (bool sourceHasName in new[] { true, false })
        {
            string tag = sourceHasName ? "mismatch_with_name" : "mismatch_without_name";
            log.Add(Probe679.X(c, $"upsert.{tag}.drop", "DROP TABLE IF EXISTS pt679, ps679"));
            log.Add(Probe679.X(c, $"upsert.{tag}.target", "CREATE TABLE pt679 (id INT PRIMARY KEY, name VARCHAR(20))"));
            log.Add(Probe679.X(
                c, $"upsert.{tag}.source",
                sourceHasName
                    ? "CREATE TABLE ps679 (id INT, name VARCHAR(20), label VARCHAR(20))"
                    : "CREATE TABLE ps679 (id INT, label VARCHAR(20))"));
            log.Add(Probe679.X(c, $"upsert.{tag}.seed_t", "INSERT INTO pt679 VALUES (1, 'old1'), (2, 'old2')"));
            log.Add(Probe679.X(
                c, $"upsert.{tag}.seed_s",
                sourceHasName
                    ? "INSERT INTO ps679 VALUES (1, 'srcname1', 'label1'), (3, 'srcname3', 'label3')"
                    : "INSERT INTO ps679 VALUES (1, 'label1'), (3, 'label3')"));
            Probe679Table t = new("pt679");
            Probe679LabelTable s = new("ps679");
            log.AddRange(Probe679.B(c, $"upsert.{tag}", () =>
                ((IUpsert)InsertInto(t, t.Id, t.Name).Select(s.Id, s.Label).From(s))
                    .OnDuplicateKeyUpdate(t.Name == Excluded(t.Name)), Dbms.MySql));
            log.Add(Probe679.Q(c, $"upsert.{tag}.rows", "SELECT id, name FROM pt679 ORDER BY id"));
        }

        // The documented derived-table spelling, for comparison.
        log.Add(Probe679.X(c, "upsert.derived.reset", "DELETE FROM pt679"));
        log.Add(Probe679.X(c, "upsert.derived.seed", "INSERT INTO pt679 VALUES (1, 'old1'), (2, 'old2')"));
        log.Add(Probe679.X(
            c, "upsert.derived.run",
            "INSERT INTO pt679 (id, name) SELECT * FROM (SELECT id, name FROM ps679) AS dt "
            + "ON DUPLICATE KEY UPDATE name = dt.name"));
        log.Add(Probe679.Q(c, "upsert.derived.rows", "SELECT id, name FROM pt679 ORDER BY id"));
        log.Add(Probe679.X(c, "upsert.drop", "DROP TABLE IF EXISTS pt679, ps679"));

        // The defaults row the empty INSERT guards reject.
        log.Add(Probe679.X(
            c, "defaults.create",
            "CREATE TABLE pd679 (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(20) DEFAULT 'd')"));
        log.Add(Probe679.X(c, "defaults.cols_values", "INSERT INTO pd679 () VALUES ()"));
        log.Add(Probe679.X(c, "defaults.values", "INSERT INTO pd679 VALUES ()"));
        log.Add(Probe679.X(c, "defaults.values_two_rows", "INSERT INTO pd679 VALUES (), ()"));
        log.Add(Probe679.X(c, "defaults.set_empty", "INSERT INTO pd679 SET"));
        log.Add(Probe679.Q(c, "defaults.rows", "SELECT id, name FROM pd679 ORDER BY id"));
        log.Add(Probe679.X(c, "defaults.drop", "DROP TABLE pd679"));

        // A joined DELETE whose target is re-listed through a join, not in FROM's list.
        using (IDbTransaction tx = c.BeginTransaction())
        {
            log.Add(Probe679.X(
                c, "relist_join.delete",
                "DELETE `x` FROM users `y` INNER JOIN orders `x` ON `x`.user_id = `y`.id WHERE `y`.id = 1",
                tx));
            log.Add(Probe679.X(
                c, "relist_from.delete",
                "DELETE `x` FROM orders `x` INNER JOIN users `y` ON `x`.user_id = `y`.id WHERE `y`.id = 2",
                tx));
            log.Add(Probe679.Q(c, "relist.orders_left", "SELECT user_id, COUNT(*) FROM orders GROUP BY user_id ORDER BY user_id", tx));
            tx.Rollback();
        }

        Probe679.Report("MySql", log);
    }
}

public abstract class OracleProbe679Base
{
    protected static List<string> Collect(IDbConnection c)
    {
        List<string> log = new();
        log.Add(Probe679.Q(c, "version", "SELECT banner_full FROM v$version"));

        // An output variable "0 " beside the positional input :0.
        UsersTable u = new();
        using (IDbTransaction tx = c.BeginTransaction())
        {
            ISqlBuilder Returning() =>
                Update(u).Set(u.Age == 77).Where(u.Id == 1)
                    .Returning(u.Id, u.Name)
                    .Into(new OutputParameter("0 ", DbType.Int32), new OutputParameter("x", DbType.String, 100));
            log.Add($"outparam.text: {Returning().Build(Dbms.Oracle).Text}");
            try
            {
                DynamicParameters outputs = c.ExecuteReturningInto(Returning(), tx);
                IEnumerable<string> names = outputs.ParameterNames.Select(n =>
                {
                    object? v = outputs.Get<object>(n);
                    return $"{n}=[{v}]";
                });
                log.Add($"outparam.run: ok, {string.Join(" ", names)}");
            }
            catch (Exception e)
            {
                log.Add($"outparam.run: ERROR {e.GetType().Name}: {e.Message.Split('\n')[0]}");
            }

            log.Add(Probe679.Q(c, "outparam.row1", "SELECT id, age FROM users WHERE id = 1", tx));
            tx.Rollback();
        }

        // DELETE with a second FROM, the opener Oracle's from_using_clause takes.
        using (IDbTransaction tx = c.BeginTransaction())
        {
            log.Add(Probe679.X(
                c, "delete.from_from",
                "DELETE FROM orders x FROM users y WHERE x.user_id = y.id AND y.id = 1", tx));
            log.Add(Probe679.X(
                c, "delete.from_using",
                "DELETE FROM orders x USING users y WHERE x.user_id = y.id AND y.id = 2", tx));
            log.Add(Probe679.Q(c, "delete.orders_left", "SELECT user_id, COUNT(*) FROM orders GROUP BY user_id ORDER BY user_id", tx));
            tx.Rollback();
        }

        // AS before a table alias.
        log.Add(Probe679.Q(c, "table_alias.as", "SELECT COUNT(*) FROM users AS u"));
        log.Add(Probe679.Q(c, "table_alias.bare", "SELECT COUNT(*) FROM users u"));
        return log;
    }
}

[Trait("Probe", "Oracle")]
public sealed class OracleProbe679 : OracleProbe679Base, IClassFixture<OracleFixture>
{
    private readonly OracleFixture _f;

    public OracleProbe679(OracleFixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe679.Report("Oracle", Collect(c));
    }
}

[Trait("Probe", "Oracle23ai")]
public sealed class Oracle23aiProbe679 : OracleProbe679Base, IClassFixture<Oracle23aiFixture>
{
    private readonly Oracle23aiFixture _f;

    public Oracle23aiProbe679(Oracle23aiFixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe()
    {
        using IDbConnection c = _f.OpenConnection();
        Probe679.Report("Oracle23ai", Collect(c));
    }
}

[Trait("Probe", "SqlServer")]
public sealed class SqlServerProbe679 : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _f;

    public SqlServerProbe679(SqlServerFixture f)
    {
        _f = f;
    }

    [Fact]
    public void Observe()
    {
        List<string> log = new();
        using IDbConnection c = _f.OpenConnection();
        log.Add(Probe679.Q(c, "version", "SELECT SERVERPROPERTY('ProductVersion')"));
        using (IDbTransaction tx = c.BeginTransaction())
        {
            log.Add(Probe679.X(
                c, "relist_join.update",
                "UPDATE [x] SET amount = 1 FROM users [y] INNER JOIN orders [x] ON [x].user_id = [y].id WHERE [y].id = 1",
                tx));
            log.Add(Probe679.X(
                c, "relist_join.delete",
                "DELETE [x] FROM users [y] INNER JOIN orders [x] ON [x].user_id = [y].id WHERE [y].id = 2",
                tx));
            log.Add(Probe679.Q(c, "relist.orders", "SELECT user_id, COUNT(*), SUM(amount) FROM orders GROUP BY user_id ORDER BY user_id", tx));
            log.Add(Probe679.X(
                c, "output_into.alias",
                "UPDATE users SET age = age OUTPUT INSERTED.id, INSERTED.name INTO output_archive AS a WHERE id = 1",
                tx));
            log.Add(Probe679.X(
                c, "output_into.bare",
                "UPDATE users SET age = age OUTPUT INSERTED.id, INSERTED.name INTO output_archive WHERE id = 1",
                tx));
            tx.Rollback();
        }

        Probe679.Report("SqlServer", log);
    }
}
