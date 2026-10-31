using System.Data;
using System.Data.Common;
using System.Globalization;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using SqlArtisan.IntegrationTests.ValueReading;
using Xunit.Abstractions;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.NameResolution;

/// <summary>Runs every Example on one engine, one <c>NR</c> line each, and always fails
/// so that CI prints them. OK / MISMATCH where the records decide, RECORDED otherwise.</summary>
public abstract class NameResolutionTestBase
{
    private readonly Func<IDbConnection> _open;
    private readonly Lane _lane;
    private readonly ITestOutputHelper _output;

    protected NameResolutionTestBase(Func<IDbConnection> open, Lane lane, ITestOutputHelper o)
    {
        _open = open;
        _lane = lane;
        _output = o;
    }

    [Fact]
    public void Examples_MatchTheModel()
    {
        Dbms dbms = ValueReadingModel.DbmsOf(_lane);
        using IDbConnection cnn = _open();
        Seed(cnn, dbms);

        List<string> lines = new();
        foreach (NrExample e in NameResolutionExamples.All)
        {
            NrCell cell = NameResolutionModel.Expect(e.Keys[0], _lane);
            bool supported = e.Supported?.Invoke(dbms) ?? true;
            string observed = supported ? Observe(cnn, e, dbms, out string sql) : "UNSUPPORTED";
            string expected = cell.Outcome switch
            {
                NrOutcome.Binds => e.Intended ?? "?",
                NrOutcome.BuildRejects => "BUILD-THROW",
                NrOutcome.EngineRejects => "ENGINE-ERROR",
                _ => $"({e.Intended ?? "-"} / other {e.Other ?? "-"})",
            };
            string verdict = !supported ? "DELEGATED" : Verdict(cell, e, observed);
            string line = $"NR\t{_lane}\t{e.Id}\t{cell.Outcome}{Q(cell)}\t{expected}"
                + $"\t{observed}\t{verdict}";
            lines.Add(line);
            _output.WriteLine(line);
        }

        string? dir = Environment.GetEnvironmentVariable("VR_RESULTS_DIR");
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, $"nr-{_lane}.tsv"), lines);
        }

        // Under mutation testing the baseline replaces the always-fail: only a changed
        // line (a reading, a verdict or a message) kills the mutant.
        string? baseline = Environment.GetEnvironmentVariable("NR_BASELINE_DIR");
        if (baseline is not null)
        {
            Assert.Equal(File.ReadAllLines(Path.Combine(baseline, $"nr-{_lane}.tsv")), lines);
            return;
        }

        Assert.Fail(string.Join("\n", lines));
    }

    private static string Q(NrCell c) => c.QuestionId is null ? "" : $"[{c.QuestionId}]";

    private static string Verdict(NrCell cell, NrExample e, string observed)
    {
        bool error = observed.StartsWith("ENGINE-ERROR", StringComparison.Ordinal)
            || observed.StartsWith("DRIVER-ERROR", StringComparison.Ordinal);
        switch (cell.Outcome)
        {
            case NrOutcome.Binds:
                return observed == e.Intended ? "OK" : "MISMATCH";
            case NrOutcome.BuildRejects:
                return observed.StartsWith("BUILD-THROW", StringComparison.Ordinal)
                    ? "OK" : "MISMATCH";
            case NrOutcome.EngineRejects:
                return error ? "OK" : "MISMATCH";
            default:
                string tag = observed.StartsWith("BUILD-THROW", StringComparison.Ordinal)
                    ? "build-throw"
                    : error ? "error"
                    : observed == e.Intended ? "=intended"
                    : observed == e.Other ? "=other"
                    : "else";
                return "RECORDED:" + tag;
        }
    }

    private string Observe(IDbConnection cnn, NrExample e, Dbms dbms, out string sql)
    {
        sql = "";
        ISqlBuilder builder;
        try
        {
            sql = e.Make(dbms).Build(dbms).Text.Replace('\n', ' ');
            builder = e.Make(dbms); // one builder builds once (#676)
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return "BUILD-THROW: " + Trim(ex.Message);
        }

        _output.WriteLine($"NR-SQL\t{_lane}\t{e.Id}\t{sql}");
        using IDbTransaction tx = cnn.BeginTransaction();
        try
        {
            List<string> rows = new();
            if (e.Run == NrRun.Dml)
            {
                cnn.Execute(builder, tx);
                rows.AddRange(cnn.Query("SELECT id, b FROM nr_w ORDER BY id", transaction: tx)
                    .Select(Row));
            }
            else
            {
                rows.AddRange(cnn.Query(builder, tx).Select(Row));
            }

            if (e.Sort)
            {
                rows.Sort(StringComparer.Ordinal);
            }

            return string.Join(",", rows);
        }
        catch (DbException ex)
        {
            return "ENGINE-ERROR: " + Trim(ex.Message);
        }
        catch (Exception ex) when (ex is not (ArgumentException or InvalidOperationException))
        {
            return $"DRIVER-ERROR: {ex.GetType().Name} {Trim(ex.Message)}";
        }
        finally
        {
            try
            {
                tx.Rollback();
            }
            catch (Exception ex) when (ex is DbException or InvalidOperationException)
            {
                // a failed statement may already have ended the transaction
            }
        }
    }

    private static string Trim(string m)
    {
        string s = m.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
        return s.Length > 140 ? s[..140] : s;
    }

    private static string Row(object row) =>
        string.Join("|", ((IDictionary<string, object?>)row).Values.Select(Normalize));

    private static string Normalize(object? value) => value switch
    {
        null or DBNull => "NULL",
        string s => s.TrimEnd(),
        byte or sbyte or short or ushort or int or uint or long or ulong or decimal or float
            or double => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
                .ToString("0.############", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "?",
    };

    private static void Seed(IDbConnection cnn, Dbms dbms)
    {
        string integer = dbms == Dbms.Oracle ? "NUMBER(10)" : "INT";
        string text = dbms == Dbms.Oracle ? "VARCHAR2(10)" : "VARCHAR(10)";
        foreach (string table in new[] { "nr_t", "nr_s", "nr_w" })
        {
            try
            {
                cnn.Execute($"DROP TABLE {table}");
            }
            catch (DbException)
            {
                // absent on a fresh database
            }
        }

        cnn.Execute($"CREATE TABLE nr_t (id {integer} PRIMARY KEY, b {integer}, c {text}, "
            + $"g {integer}, k {integer})");
        cnn.Execute($"CREATE TABLE nr_s (id {integer} PRIMARY KEY, b {integer}, c {text})");
        cnn.Execute($"CREATE TABLE nr_w (id {integer} PRIMARY KEY, b {integer}, c {text})");

        DbTable t = NameResolutionExamples.Tu;
        DbTable s = NameResolutionExamples.Su;
        DbTable w = NameResolutionExamples.Wu;
        foreach ((int id, int b, string c, int g) in new[] { (1, 10, "p", 1), (2, 20, "q", 1),
            (3, 30, "r", 2) })
        {
            cnn.Execute(InsertInto(t, t.Column("id"), t.Column("b"), t.Column("c"),
                t.Column("g"), t.Column("k")).Values(id, b, c, g, id * 100));
            cnn.Execute(InsertInto(w, w.Column("id"), w.Column("b"), w.Column("c"))
                .Values(id, b, c));
        }

        foreach ((int id, int b, string c) in new[] { (1, 30, "r"), (2, 10, "q"), (3, 20, "p") })
        {
            cnn.Execute(InsertInto(s, s.Column("id"), s.Column("b"), s.Column("c"))
                .Values(id, b, c));
        }
    }
}

[Collection("NameResolution")]
[Trait("Engine", "Sqlite")]
public sealed class SqliteNameResolutionTests
    : NameResolutionTestBase, IClassFixture<SqliteFixture>
{
    public SqliteNameResolutionTests(SqliteFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Sqlite, o)
    {
    }
}

[Collection("NameResolution")]
[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlNameResolutionTests
    : NameResolutionTestBase, IClassFixture<PostgreSqlFixture>
{
    public PostgreSqlNameResolutionTests(PostgreSqlFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.PostgreSql, o)
    {
    }
}

[Collection("NameResolution")]
[Trait("Engine", "PostgreSql17")]
public sealed class PostgreSql17NameResolutionTests
    : NameResolutionTestBase, IClassFixture<PostgreSql17Fixture>
{
    public PostgreSql17NameResolutionTests(PostgreSql17Fixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.PostgreSql17, o)
    {
    }
}

[Collection("NameResolution")]
[Trait("Engine", "MySql")]
public sealed class MySqlNameResolutionTests
    : NameResolutionTestBase, IClassFixture<MySqlFixture>
{
    public MySqlNameResolutionTests(MySqlFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.MySql, o)
    {
    }
}

[Collection("NameResolution")]
[Trait("Engine", "SqlServer")]
public sealed class SqlServerNameResolutionTests
    : NameResolutionTestBase, IClassFixture<SqlServerFixture>
{
    public SqlServerNameResolutionTests(SqlServerFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.SqlServer, o)
    {
    }
}

[Collection("NameResolution")]
[Trait("Engine", "SqlServer2025")]
public sealed class SqlServer2025NameResolutionTests
    : NameResolutionTestBase, IClassFixture<SqlServer2025Fixture>
{
    public SqlServer2025NameResolutionTests(SqlServer2025Fixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.SqlServer2025, o)
    {
    }
}

[Collection("NameResolution")]
[Trait("Engine", "Oracle")]
public sealed class OracleNameResolutionTests
    : NameResolutionTestBase, IClassFixture<OracleFixture>
{
    public OracleNameResolutionTests(OracleFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Oracle, o)
    {
    }
}

[Collection("NameResolution")]
[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiNameResolutionTests
    : NameResolutionTestBase, IClassFixture<Oracle23aiFixture>
{
    public Oracle23aiNameResolutionTests(Oracle23aiFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Oracle23ai, o)
    {
    }
}

[Collection("NameResolution")]
[Trait("Engine", "LocalPostgreSql")]
public sealed class LocalPostgreSqlNameResolutionTests : NameResolutionTestBase
{
    public LocalPostgreSqlNameResolutionTests(ITestOutputHelper o)
        : base(LocalPostgreSqlValueReadingTests.Open, Lane.PostgreSql, o)
    {
    }
}

/// <summary>The model's own gates: every builder stage maps to the position axis or to
/// none on purpose, every Example key is a cell, and the dump for the report.</summary>
public sealed class NameResolutionModelTests
{
    private static readonly NrPosition[] FactoryPositions =
    [
        NrPosition.WindowPartition, NrPosition.WindowOrder, NrPosition.AggregateOrder,
    ];

    [Fact]
    public void Positions_AreReachedByAStageOrAFactory()
    {
        HashSet<NrPosition> reached = [.. ClauseInventory.Stages.SelectMany(
            c => NameResolutionModel.PositionsOf(c.Stage, c.Method, c.Signature))];
        reached.UnionWith(FactoryPositions);

        Assert.Empty(Enum.GetValues<NrPosition>().Where(p => !reached.Contains(p)));
    }

    [Fact]
    public void ExampleKeys_AreModelCells()
    {
        Assert.Empty(NameResolutionExamples.All.SelectMany(e => e.Keys)
            .Where(k => !NameResolutionModel.Applies(k)));
    }

    [Fact]
    public void Model_Dump_WritesTsv()
    {
        string? path = Environment.GetEnvironmentVariable("NR_MODEL_TSV");
        if (path is null)
        {
            return;
        }

        List<string> lines = new();
        foreach (NrKey k in NameResolutionModel.AllKeys())
        {
            foreach (Lane lane in Enum.GetValues<Lane>())
            {
                NrCell c = NameResolutionModel.Expect(k, lane);
                lines.Add($"{k.Position}\t{k.Ref}\t{k.Owner}\t{k.Shadow}\t{k.Block}\t{lane}"
                    + $"\t{c.Outcome}\t{c.QuestionId}\t{c.Basis}");
            }
        }

        File.WriteAllLines(path, lines);
    }
}
