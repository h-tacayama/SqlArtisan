using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using SqlArtisan.Dapper;
using Xunit.Abstractions;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.ValueReading;

/// <summary>The left operand's type, an axis the operator positions need: the same
/// right-hand value reads differently beside an integer, a text or a date column.</summary>
public enum LeftType
{
    Int,
    Text,
    Date,
}

public sealed record ValuePositionExample(
    string Id,
    string Family,
    string Op,
    LeftType Left,
    ValueKind Kind,
    PositionClass Class);

// E5 (re-measurement): every value position family x every value kind, operators split
// one slot each and crossed with the left operand's type; judged by cross-engine agreement.
public static class ValuePositionCatalog
{
    private static readonly string[] Comparisons =
        ["==", "!=", "<", "<=", ">", ">=", "Like", "NotLike", "Between", "NotBetween", "In",
            "NotIn"];

    private static readonly string[] Arithmetic = ["+", "-", "*", "/", "%"];

    private static readonly string[] Echoes = ["SEL", "THEN", "BIND", "ABS", "UPPER"];

    public static IReadOnlyList<ValuePositionExample> All { get; } = Derive();

    private static List<ValuePositionExample> Derive()
    {
        List<ValuePositionExample> list = new();
        foreach (ValueKind k in ValueKinds.All)
        {
            foreach (LeftType l in Enum.GetValues<LeftType>())
            {
                foreach (string op in Comparisons)
                {
                    list.Add(new($"E5-CMP{op}-{l}-{k}", "CMP", op, l, k, op switch
                    {
                        "Between" or "NotBetween" or "In" or "NotIn"
                            => PositionClass.ComparedOperand,
                        _ => PositionClass.QuantifiableOperand,
                    }));
                }

                foreach (string op in Arithmetic)
                {
                    list.Add(new($"E5-ARITH{op}-{l}-{k}", "ARITH", op, l, k, op switch
                    {
                        "+" => PositionClass.PlusOperand,
                        "-" => PositionClass.MinusOperand,
                        _ => PositionClass.ArithmeticOperand,
                    }));
                }

                list.Add(new($"E5-CASE-{l}-{k}", "CASE", "", l, k,
                    PositionClass.SimpleCaseOperand));
                list.Add(new($"E5-COALESCE-{l}-{k}", "COALESCE", "", l, k,
                    PositionClass.ValueArgument));
                list.Add(new($"E5-INS-{l}-{k}", "INS", "", l, k, PositionClass.InsertValue));
                list.Add(new($"E5-SET-{l}-{k}", "SET", "", l, k,
                    PositionClass.QuantifiableOperand));

                // SET X = v, X2 = X: does the second assignment read X before or after the
                // first wrote it (F4)? ODKU is the same pair under ON DUPLICATE KEY UPDATE.
                list.Add(new($"E5-SET2-{l}-{k}", "SET2", "", l, k,
                    PositionClass.QuantifiableOperand));
                list.Add(new($"E5-ODKU-{l}-{k}", "ODKU", "", l, k,
                    PositionClass.QuantifiableOperand));
            }

            foreach (string echo in Echoes)
            {
                list.Add(new($"E5-{echo}-{k}", echo, "", LeftType.Int, k, echo switch
                {
                    "SEL" => PositionClass.SelectItem,
                    "THEN" => PositionClass.CaseResult,
                    "BIND" => PositionClass.ExplicitBind,
                    _ => PositionClass.ValueArgument,
                }));
            }
        }

        return list;
    }

    internal static SqlExpression LeftOf(DbTable t, LeftType l) => l switch
    {
        LeftType.Int => t.Column("b"),
        LeftType.Text => t.Column("a"),
        _ => t.Column("d"),
    };

    private static string WriteColumn(LeftType l) => l switch
    {
        LeftType.Int => "i",
        LeftType.Text => "s",
        _ => "d",
    };

    /// <summary>The query that reads the result back; for INS / SET the write itself
    /// comes from <see cref="Write"/>.</summary>
    internal static ISqlBuilder Read(ValuePositionExample e)
    {
        DbTable t = new("vr_t", "t");
        DbTable w = new("vr_w");
        object v = ValueKinds.Sample(e.Kind, t)!;
        SqlExpression left = LeftOf(t, e.Left);

        return e.Family switch
        {
            "CMP" => Select(t.Column("id")).From(t).Where(Compare(left, e.Op, v))
                .OrderBy(t.Column("id")),
            "ARITH" => Select(t.Column("id"), Arith(left, e.Op, v)).From(t)
                .OrderBy(t.Column("id")),
            "CASE" => Select(t.Column("id"), Case(left, When(v).Then("hit"), Else("miss")))
                .From(t).OrderBy(t.Column("id")),
            "COALESCE" => Select(t.Column("id"), Coalesce(left, v)).From(t)
                .OrderBy(t.Column("id")),
            "INS" => Select(w.Column(WriteColumn(e.Left))).From(w).Where(w.Column("id") == 100),
            "SET" => Select(w.Column(WriteColumn(e.Left))).From(w).Where(w.Column("id") == 1),
            "SET2" or "ODKU" => Select(w.Column(WriteColumn(e.Left)),
                    w.Column(WriteColumn(e.Left) + "2"))
                .From(w).Where(w.Column("id") == 1),
            "SEL" => Select(v).From(t).Where(t.Column("id") == 1),
            "THEN" => Select(Case(When(t.Column("id") == 1).Then(v))).From(t)
                .Where(t.Column("id") == 1),
            "BIND" => Select(Bind(v)).From(t).Where(t.Column("id") == 1),
            "ABS" => Select(Abs(v)).From(t).Where(t.Column("id") == 1),
            _ => Select(Upper(v)).From(t).Where(t.Column("id") == 1),
        };
    }

    internal static ISqlBuilder Write(ValuePositionExample e)
    {
        DbTable w = new("vr_w");
        object v = ValueKinds.Sample(e.Kind, new DbTable("vr_t", "t"))!;
        DbColumn col = w.Column(WriteColumn(e.Left));
        DbColumn col2 = w.Column(WriteColumn(e.Left) + "2");
        return e.Family switch
        {
            "INS" => InsertInto(w, w.Column("id"), col).Values(100, v),
            "SET2" => Update(w).Set(col == v, col2 == col).Where(w.Column("id") == 1),
            "ODKU" => InsertInto(w, w.Column("id"), col).Values(1, v)
                .OnDuplicateKeyUpdate(col == v, col2 == col),
            _ => Update(w).Set(col == v).Where(w.Column("id") == 1),
        };
    }

    private static SqlCondition Compare(SqlExpression l, string op, object v) => op switch
    {
        "==" => l == v,
        "!=" => l != v,
        "<" => l < v,
        "<=" => l <= v,
        ">" => l > v,
        ">=" => l >= v,
        "Like" => l.Like(v),
        "NotLike" => l.NotLike(v),
        "Between" => l.Between(v, v),
        "NotBetween" => l.NotBetween(v, v),
        "In" => l.In([v]),
        _ => l.NotIn([v]),
    };

    private static SqlExpression Arith(SqlExpression l, string op, object v) => op switch
    {
        "+" => l + v,
        "-" => l - v,
        "*" => l * v,
        "/" => l / v,
        _ => l % v,
    };
}

/// <summary>Runs E5 on one engine and compares each observation with the committed
/// PostgreSQL 16 baseline; prints the summary and every difference that returns rows.</summary>
public abstract class ValuePositionTestBase
{
    private readonly Func<IDbConnection> _open;
    private readonly Lane _lane;
    private readonly ITestOutputHelper _output;

    protected ValuePositionTestBase(Func<IDbConnection> open, Lane lane, ITestOutputHelper output)
    {
        _open = open;
        _lane = lane;
        _output = output;
    }

    [Fact]
    public void ValuePositions_AgreeWithTheBaseline()
    {
        Dbms dbms = ValueReadingModel.DbmsOf(_lane);
        using IDbConnection cnn = _open();
        Seed(cnn, dbms);

        Dictionary<string, string> baseline = LoadBaseline();
        List<string> lines = new();
        List<string> printed = new();
        Dictionary<string, int> counts = new();
        SortedDictionary<string, StringBuilder> signatures = new(StringComparer.Ordinal);
        StringBuilder all = new();

        foreach (ValuePositionExample e in ValuePositionCatalog.All)
        {
            string observed = Observe(cnn, e, dbms);
            string? expected = ValuePositionOracle.Expected(e);
            Cell cell = ValueReadingModel.Expect(e.Class, e.Kind, _lane,
                e.Family is "SET" or "SET2" or "ODKU" ? Context.SetClause : Context.TopLevel);
            all.Append(e.Id).Append('=').Append(observed).Append('\n');

            // Build-time outcomes are the DB-less layer's; here only what reaches the engine.
            string verdict = observed == "BUILD-THROW"
                ? (cell.Outcome == Outcome.BuildRejects ? "BUILD-AS-MODEL" : "BUILD-UNEXPECTED")
                : expected is null ? (IsError(observed) ? "NO-ORACLE-ERROR" : "NO-ORACLE-VALUE")
                : IsError(observed) ? "ORACLE-LOUD"
                : observed == expected ? "ORACLE-MATCH"
                : "ORACLE-MISMATCH";
            counts[verdict] = counts.GetValueOrDefault(verdict) + 1;
            lines.Add($"{e.Id}\t{verdict}\t{expected}\t{observed}");

            if (verdict is "ORACLE-MISMATCH" or "BUILD-UNEXPECTED"
                || (verdict == "ORACLE-LOUD" && e.Family == "ODKU" && dbms == Dbms.MySql))
            {
                printed.Add(
                    $"VR5\t{_lane}\t{verdict}\t{e.Id}\t{Short(expected)}\t{Short(observed)}");
            }

            // One character pair per kind: V/E/B/D, then two hex digits of the value's hash,
            // so lanes can be compared offline without printing every value.
            string group = $"{e.Family}{e.Op}-{e.Left}";
            if (!signatures.TryGetValue(group, out StringBuilder? sig))
            {
                signatures[group] = sig = new StringBuilder();
            }

            sig.Append(expected is null ? Code(observed) : "-").Append(Hash2(observed)).Append(' ');
            _ = baseline;
        }

        string hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(all.ToString())))[..12];
        string summary = $"VR5-SUMMARY\t{_lane}\ttotal={lines.Count}\thash={hash}\t"
            + string.Join("\t", counts.OrderBy(c => c.Key).Select(c => $"{c.Key}={c.Value}"));
        _output.WriteLine(summary);
        foreach (string p in printed)
        {
            _output.WriteLine(p);
        }

        foreach ((string group, StringBuilder sig) in signatures)
        {
            string line = $"VR5-SIG\t{_lane}\t{group}\t{sig}";
            printed.Add(line);
            _output.WriteLine(line);
        }

        string? dir = Environment.GetEnvironmentVariable("VR_RESULTS_DIR");
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, $"e5-{_lane}.tsv"), lines);
        }

        // Always fails, so the runner prints the measurement; the verdicts are in the lines.
        Assert.Fail(summary + "\n" + string.Join("\n", printed));
    }

    private static string Code(string s) => s.StartsWith("ENGINE-ERROR", StringComparison.Ordinal)
        ? "E" : s.StartsWith("BUILD", StringComparison.Ordinal) ? "B"
        : s.StartsWith("DRIVER", StringComparison.Ordinal) ? "D" : "V";

    private static string Hash2(string s) => IsError(s) ? "--"
        : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)))[..2];

    private static bool IsError(string? s) => s is not null
        && (s.StartsWith("ENGINE-ERROR", StringComparison.Ordinal)
            || s.StartsWith("BUILD-THROW", StringComparison.Ordinal)
            || s.StartsWith("DRIVER-ERROR", StringComparison.Ordinal));

    private static string Short(string? s) =>
        s is null ? "-" : s.Length > 70 ? s[..70] : s;

    private static Dictionary<string, string> LoadBaseline()
    {
        string dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "SqlArtisan.sln")))
        {
            dir = Path.GetDirectoryName(dir)
                ?? throw new InvalidOperationException("repository root not found");
        }

        string path = Path.Combine(dir, "trials", "value-reading", "e5-baseline-pg16.tsv");
        return File.Exists(path)
            ? File.ReadLines(path).Select(l => l.Split('\t', 2))
                .ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : "")
            : new Dictionary<string, string>();
    }

    private static string Observe(IDbConnection cnn, ValuePositionExample e, Dbms dbms)
    {
        bool writes = e.Family is "INS" or "SET" or "SET2" or "ODKU";
        try
        {
            (writes ? ValuePositionCatalog.Write(e) : ValuePositionCatalog.Read(e)).Build(dbms);
            ValuePositionCatalog.Read(e).Build(dbms);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return "BUILD-THROW";
        }

        IDbTransaction? tx = writes ? cnn.BeginTransaction() : null;
        try
        {
            if (writes)
            {
                cnn.Execute(ValuePositionCatalog.Write(e), tx);
            }

            List<string> rows = new();
            foreach (object row in cnn.Query(ValuePositionCatalog.Read(e), tx))
            {
                rows.Add(string.Join(":", ((IDictionary<string, object?>)row).Values
                    .Select(Normalize)));
            }

            return string.Join(",", rows);
        }
        catch (DbException ex)
        {
            string m = ex.Message.Replace('\t', ' ').Replace('\n', ' ');
            return "ENGINE-ERROR: " + (m.Length > 60 ? m[..60] : m);
        }
        catch (Exception ex)
        {
            return $"DRIVER-ERROR: {ex.GetType().Name}";
        }
        finally
        {
            tx?.Rollback();
            tx?.Dispose();
        }
    }

    private static string Normalize(object? value) => value switch
    {
        null or DBNull => "NULL",
        bool b => b ? "1" : "0",
        string s => s.TrimEnd(),
        byte or sbyte or short or ushort or int or uint or long or ulong or decimal or float
            or double => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
                .ToString("0.############", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "?",
    };

    private static void Seed(IDbConnection cnn, Dbms dbms)
    {
        string integer = dbms == Dbms.Oracle ? "NUMBER(10)" : "INT";
        string text = dbms == Dbms.Oracle ? "VARCHAR2(40)" : "VARCHAR(40)";
        string stamp = dbms switch
        {
            Dbms.SqlServer => "DATETIME2",
            Dbms.MySql => "DATETIME",
            _ => "TIMESTAMP",
        };

        foreach (string table in new[] { "vr_t", "vr_s", "vr_w" })
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

        cnn.Execute($"CREATE TABLE vr_t (id {integer} PRIMARY KEY, a {text}, b {integer}, "
            + $"g {integer}, d {stamp})");
        cnn.Execute($"CREATE TABLE vr_s (id {integer} PRIMARY KEY, b {integer})");
        cnn.Execute($"CREATE TABLE vr_w (id {integer} PRIMARY KEY, i {integer}, s {text}, "
            + $"d {stamp}, i2 {integer}, s2 {text}, d2 {stamp})");

        DbTable t = new("vr_t");
        foreach ((int id, string a, int b, int g) in ExampleCatalog.Rows)
        {
            cnn.Execute(InsertInto(
                    t, t.Column("id"), t.Column("a"), t.Column("b"), t.Column("g"), t.Column("d"))
                .Values(id, a, b, g, new DateTime(2024, 1, id, 10, 0, 0)));
        }

        DbTable s = new("vr_s");
        foreach ((int id, int b) in ExampleCatalog.SideRows)
        {
            cnn.Execute(InsertInto(s, s.Column("id"), s.Column("b")).Values(id, b));
        }

        DbTable w = new("vr_w");
        cnn.Execute(InsertInto(
                w, w.Column("id"), w.Column("i"), w.Column("s"), w.Column("d"), w.Column("i2"),
                w.Column("s2"), w.Column("d2"))
            .Values(1, 10, "x", new DateTime(2024, 1, 1, 10, 0, 0), 0, "y",
                new DateTime(2000, 1, 1, 0, 0, 0)));
    }
}
