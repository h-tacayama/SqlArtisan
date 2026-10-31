using System.Data;
using System.Data.Common;
using System.Globalization;
using Dapper;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;
using Xunit.Abstractions;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.ValueReading;

/// <summary>One measurement: a family, an id, and the query; no single intended result
/// is asserted — the lanes' answers are compared afterwards (family-proposals.md).</summary>
public sealed record FamilyCase(string Family, string Id, Func<Dbms, ISqlBuilder> Make);

// Family 4 (an integer key past int.MaxValue) and family 11 (integer arguments whose
// meaning differs by engine), measured on every lane before either is decided.
public static class FamilyCases
{
    private static readonly DbTable T = new("fm_t", "t");
    private static readonly DbTable One = new("fm_one", "o");

    private static readonly object[] Keys = [2, 99, 2147483647, 2147483648L, long.MaxValue];
    private static readonly object[] Ints = [0, -1, -2, 1, 2, 100, 2.5];
    private static readonly object[] Small = [0, -1, 1, 2, 2.5];

    public static IReadOnlyList<FamilyCase> All { get; } = Build();

    private static string Name(object v) => v switch
    {
        long.MaxValue => "LongMax",
        double d => d.ToString("0.0", CultureInfo.InvariantCulture),
        _ => Convert.ToString(v, CultureInfo.InvariantCulture)!,
    };

    private static ISqlBuilder Scalar(SqlExpression e) => Select(e).From(One);

    private static List<FamilyCase> Build()
    {
        List<FamilyCase> list = new();
        DbColumn id = T.Column("id");
        DbColumn b = T.Column("b");

        foreach (object k in Keys)
        {
            string n = Name(k);
            list.Add(new("4", $"ORD-{n}", _ => Select(id, b).From(T).OrderBy(k, id)));
            list.Add(new("4", $"GRP-{n}", _ => Select(Count(id)).From(T).GroupBy(k)));
            list.Add(new("4", $"CORD-{n}", _ => Select(id, b).From(T).Where(id <= 2)
                .UnionAll.Select(id, b).From(T).Where(id > 2).OrderBy(k, id)));
            list.Add(new("4", $"DON-{n}", _ => Select(DistinctOn(k), id).From(T).OrderBy(k, id)));
            list.Add(new("4", $"PART-{n}", _ => Select(id, RowNumber().Over(PartitionBy(k)
                .OrderBy(id))).From(T).OrderBy(id)));
        }

        foreach (object v in Ints)
        {
            string n = Name(v);
            list.Add(new("11", $"SUBSTR-pos-{n}", _ => Scalar(Substr("abcde", v))));
            list.Add(new("11", $"SUBSTR-len-{n}", _ => Scalar(Substr("abcde", 2, v))));
            list.Add(new("11", $"SUBSTRING-pos-{n}", _ => Scalar(Substring("abcde", v, 2))));
            list.Add(new("11", $"SUBSTRING-len-{n}", _ => Scalar(Substring("abcde", 2, v))));
            list.Add(new("11", $"LEFT-{n}", _ => Scalar(Left("abcde", v))));
            list.Add(new("11", $"RIGHT-{n}", _ => Scalar(Right("abcde", v))));
            list.Add(new("11", $"LPAD-{n}", _ => Scalar(Lpad("abc", v, "x"))));
            list.Add(new("11", $"RPAD-{n}", _ => Scalar(Rpad("abc", v, "x"))));
            list.Add(new("11", $"INSTR-pos-{n}", _ => Scalar(Instr("abcabc", "b", v))));
            list.Add(new("11", $"RCOUNT-pos-{n}", _ => Scalar(RegexpCount("abcabc", "b", v))));
            list.Add(new("11", $"RSUBSTR-pos-{n}", _ => Scalar(RegexpSubstr("abcabc", "b.", v))));
            list.Add(new("11", $"RREPLACE-pos-{n}",
                _ => Scalar(RegexpReplace("abcabc", "b", "X", v))));
            list.Add(new("11", $"TRUNC-{n}", _ => Scalar(Trunc(75.65m, v))));
            list.Add(new("11", $"ROUND-{n}", _ => Scalar(Round(75.65m, v))));
        }

        foreach (object v in Small)
        {
            string n = Name(v);
            list.Add(new("11", $"RSUBSTR-occ-{n}",
                _ => Scalar(RegexpSubstr("abcabc", "b.", 1, v))));
            list.Add(new("11", $"RREPLACE-occ-{n}",
                _ => Scalar(RegexpReplace("abcabc", "b", "X", 1, v))));
            list.Add(new("11", $"RINSTR-occ-{n}",
                _ => Scalar(RegexpInstr("abcabc", "b", 1, v, 0))));
            list.Add(new("11", $"RINSTR-ret-{n}",
                _ => Scalar(RegexpInstr("abcabc", "b", 1, 1, v))));
            list.Add(new("11", $"MOD-div-{n}", _ => Scalar(Mod(7, v))));
            list.Add(new("11", $"POWER-exp-{n}", _ => Scalar(Power(2, v))));
        }

        list.Add(new("11", "MOD-neg", _ => Scalar(Mod(-7, 3))));
        list.Add(new("11", "POWER-zero-neg", _ => Scalar(Power(0, -1))));
        return list;
    }
}

/// <summary>Prints <c>FM lane family id observed</c> for every case; always fails so
/// that CI prints the lines.</summary>
public abstract class FamilyMeasureTestBase
{
    private readonly Func<IDbConnection> _open;
    private readonly Lane _lane;
    private readonly ITestOutputHelper _output;

    protected FamilyMeasureTestBase(Func<IDbConnection> open, Lane lane, ITestOutputHelper o)
    {
        _open = open;
        _lane = lane;
        _output = o;
    }

    [Fact]
    public void Cases_PrintEachLanesAnswer()
    {
        Dbms dbms = ValueReadingModel.DbmsOf(_lane);
        using IDbConnection cnn = _open();
        Seed(cnn, dbms);

        List<string> lines = new();
        foreach (FamilyCase c in FamilyCases.All)
        {
            string line = $"FM\t{_lane}\t{c.Family}\t{c.Id}\t{Observe(cnn, c, dbms)}";
            lines.Add(line);
            _output.WriteLine(line);
        }

        string? dir = Environment.GetEnvironmentVariable("VR_RESULTS_DIR");
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, $"fm-{_lane}.tsv"), lines);
        }

        Assert.Fail(string.Join("\n", lines));
    }

    private static string Observe(IDbConnection cnn, FamilyCase c, Dbms dbms)
    {
        ISqlBuilder builder;
        try
        {
            c.Make(dbms).Build(dbms);
            builder = c.Make(dbms); // one builder builds once (#676)
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return "BUILD-THROW: " + Trim(ex.Message);
        }

        try
        {
            List<string> rows = [.. cnn.Query(builder).Select(Row)];
            return c.Family == "4" && c.Id.StartsWith("GRP", StringComparison.Ordinal)
                ? $"#groups={rows.Count}"
                : c.Family == "4" && c.Id.StartsWith("DON", StringComparison.Ordinal)
                    ? $"#rows={rows.Count}"
                    : string.Join(",", rows);
        }
        catch (DbException ex)
        {
            return "ENGINE-ERROR: " + Trim(ex.Message);
        }
        catch (Exception ex) when (ex is not (ArgumentException or InvalidOperationException))
        {
            return $"DRIVER-ERROR: {ex.GetType().Name} {Trim(ex.Message)}";
        }
    }

    // A key query's rows read as their id sequence, the reading the key decides.
    private static string Row(object row) =>
        Normalize(((IDictionary<string, object?>)row).Values.First());

    private static string Trim(string m)
    {
        string s = m.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
        return s.Length > 120 ? s[..120] : s;
    }

    private static string Normalize(object? value) => value switch
    {
        null or DBNull => "NULL",
        string s => s.Length == 0 ? "''" : s.TrimEnd(),
        byte or sbyte or short or ushort or int or uint or long or ulong or decimal or float
            or double => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
                .ToString("0.############", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "?",
    };

    private static void Seed(IDbConnection cnn, Dbms dbms)
    {
        string integer = dbms == Dbms.Oracle ? "NUMBER(10)" : "INT";
        foreach (string table in new[] { "fm_t", "fm_one" })
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

        cnn.Execute($"CREATE TABLE fm_t (id {integer} PRIMARY KEY, b {integer})");
        cnn.Execute($"CREATE TABLE fm_one (id {integer} PRIMARY KEY)");
        cnn.Execute("INSERT INTO fm_one (id) VALUES (1)");
        foreach ((int id, int b) in new[] { (1, 50), (2, 40), (3, 30), (4, 20), (5, 10) })
        {
            cnn.Execute($"INSERT INTO fm_t (id, b) VALUES ({id}, {b})");
        }
    }
}

[Collection("ValueReading")]
[Trait("Engine", "Sqlite")]
public sealed class SqliteFamilyMeasureTests
    : FamilyMeasureTestBase, IClassFixture<SqliteFixture>
{
    public SqliteFamilyMeasureTests(SqliteFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Sqlite, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "PostgreSql")]
public sealed class PostgreSqlFamilyMeasureTests
    : FamilyMeasureTestBase, IClassFixture<PostgreSqlFixture>
{
    public PostgreSqlFamilyMeasureTests(PostgreSqlFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.PostgreSql, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "PostgreSql17")]
public sealed class PostgreSql17FamilyMeasureTests
    : FamilyMeasureTestBase, IClassFixture<PostgreSql17Fixture>
{
    public PostgreSql17FamilyMeasureTests(PostgreSql17Fixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.PostgreSql17, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "MySql")]
public sealed class MySqlFamilyMeasureTests
    : FamilyMeasureTestBase, IClassFixture<MySqlFixture>
{
    public MySqlFamilyMeasureTests(MySqlFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.MySql, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "SqlServer")]
public sealed class SqlServerFamilyMeasureTests
    : FamilyMeasureTestBase, IClassFixture<SqlServerFixture>
{
    public SqlServerFamilyMeasureTests(SqlServerFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.SqlServer, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "SqlServer2025")]
public sealed class SqlServer2025FamilyMeasureTests
    : FamilyMeasureTestBase, IClassFixture<SqlServer2025Fixture>
{
    public SqlServer2025FamilyMeasureTests(SqlServer2025Fixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.SqlServer2025, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "Oracle")]
public sealed class OracleFamilyMeasureTests
    : FamilyMeasureTestBase, IClassFixture<OracleFixture>
{
    public OracleFamilyMeasureTests(OracleFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Oracle, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiFamilyMeasureTests
    : FamilyMeasureTestBase, IClassFixture<Oracle23aiFixture>
{
    public Oracle23aiFamilyMeasureTests(Oracle23aiFixture f, ITestOutputHelper o)
        : base(f.OpenConnection, Lane.Oracle23ai, o)
    {
    }
}

[Collection("ValueReading")]
[Trait("Engine", "LocalPostgreSql")]
public sealed class LocalPostgreSqlFamilyMeasureTests : FamilyMeasureTestBase
{
    public LocalPostgreSqlFamilyMeasureTests(ITestOutputHelper o)
        : base(LocalPostgreSqlValueReadingTests.Open, Lane.PostgreSql, o)
    {
    }
}
