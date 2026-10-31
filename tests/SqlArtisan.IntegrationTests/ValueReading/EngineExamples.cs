using System.Data;
using System.Data.Common;
using System.Globalization;
using Dapper;
using SqlArtisan.Dapper;
using Xunit.Abstractions;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.ValueReading;

/// <summary>One engine Example: a template over one position, one value kind and one
/// context, whose discriminating data returns different rows for each reading.</summary>
public sealed record EngineExample(
    string Id,
    PositionClass Class,
    ValueKind Kind,
    Context Context,
    string Template);

/// <summary>
/// Derives the engine Examples from the model by rule (see the report § Derivation):
/// E1 every key class x every value kind that reaches the engine on some lane;
/// E2 the value-domain and operand cells (string `+`, ROUND scale, ANY, Sql.Null) and the
/// select-item control over every kind; E3 the contexts the known examples raised
/// (#521 CTE / nested block, #757 compound, #678/#693 window and aggregate orderings).
/// </summary>
public static class ExampleCatalog
{
    // id | a | b | g — column 2 (b) sorts opposite to id, and g makes three groups,
    // so an ordinal, a constant and a column each return different rows.
    internal static readonly (int Id, string A, int B, int G)[] Rows =
        [(1, "e", 50, 1), (2, "d", 40, 1), (3, "c", 30, 2), (4, "b", 20, 2), (5, "a", 10, 3)];

    internal static readonly (int Id, int B)[] SideRows = [(1, 40), (2, 20)];

    private static readonly (PositionClass Class, string Template)[] KeyTemplates =
    [
        (PositionClass.StatementOrderByKey, "ORD"),
        (PositionClass.ExpressionOrderByKey, "ORDX-WINDOW"),
        (PositionClass.StatementGroupByKey, "GRP"),
        (PositionClass.GroupingElement, "GEL"),
        (PositionClass.PartitionKey, "PART"),
        (PositionClass.DistinctOnKey, "DON"),
    ];

    public static IReadOnlyList<EngineExample> All { get; } = Derive();

    private static List<EngineExample> Derive()
    {
        List<EngineExample> list = new();

        // E1: key classes x kinds that build somewhere.
        foreach ((PositionClass c, string template) in KeyTemplates)
        {
            foreach (ValueKind kind in ValueKinds.All.Where(k => ReachesEngine(c, k)))
            {
                list.Add(new($"E1-{template}-{kind}", c, kind, Context.TopLevel, template));
            }
        }

        // E2: the value-domain / operand cells, and the select-item control.
        foreach (ValueKind kind in ValueKinds.All
            .Where(k => ReachesEngine(PositionClass.SelectItem, k)))
        {
            list.Add(new(
                $"E2-SEL-{kind}", PositionClass.SelectItem, kind, Context.TopLevel, "SEL"));
        }

        foreach (ValueKind kind in new[] { ValueKind.Quantified, ValueKind.SqlNull,
            ValueKind.Column, ValueKind.BindInt })
        {
            list.Add(new($"E2-EQ-{kind}", PositionClass.QuantifiableOperand, kind,
                Context.TopLevel, "EQ"));
        }

        foreach (ValueKind kind in new[] { ValueKind.Text, ValueKind.Char })
        {
            list.Add(new($"E2-PLUS-{kind}", PositionClass.PlusOperand, kind, Context.TopLevel,
                "PLUS"));
        }

        foreach (ValueKind kind in new[] { ValueKind.IntNegative, ValueKind.IntPositive })
        {
            list.Add(new($"E2-ROUND-{kind}", PositionClass.RoundScale, kind, Context.TopLevel,
                "ROUND"));
        }

        // E4 (added after step 5): `+` / `-` beside a date with a TimeSpan, and a SET
        // assignment reading a column an earlier assignment wrote.
        list.Add(new("E4-PLUSDATE-TimeSpan", PositionClass.PlusOperand, ValueKind.TimeSpan,
            Context.TopLevel, "PLUSDATE"));
        list.Add(new("E4-MINUSDATE-TimeSpan", PositionClass.MinusOperand, ValueKind.TimeSpan,
            Context.TopLevel, "MINUSDATE"));
        list.Add(new("E4-SETSWAP-Column", PositionClass.QuantifiableOperand, ValueKind.Column,
            Context.SetClause, "SETSWAP"));

        // E3: contexts raised by the known examples.
        foreach (Context ctx in new[] { Context.Subquery, Context.Cte, Context.SetOperationBranch })
        {
            foreach (ValueKind kind in new[] { ValueKind.IntPositive, ValueKind.BindInt,
                ValueKind.Double, ValueKind.DoubleWhole, ValueKind.IntNegative, ValueKind.SqlNull,
                ValueKind.Column })
            {
                list.Add(new($"E3-ORD-{ctx}-{kind}", PositionClass.StatementOrderByKey, kind, ctx,
                    "ORD"));
            }
        }

        foreach (ValueKind kind in new[] { ValueKind.BindInt, ValueKind.SqlNull, ValueKind.Column })
        {
            list.Add(new($"E3-ORDX-AGG-{kind}", PositionClass.ExpressionOrderByKey, kind,
                Context.AggregateOrderBy, "ORDX-AGG"));
        }

        return list;
    }

    // A kind reaches an engine when some lane's cell builds (it is not a build-time
    // rejection or a compile stop, which the DB-less layer already covers).
    private static bool ReachesEngine(PositionClass c, ValueKind k) =>
        ValueReadingModel.Lanes.Any(l => ValueReadingModel.Expect(c, k, l).Outcome
            is Outcome.Executes or Outcome.AnalyzerWarns or Outcome.EngineRejects
            or Outcome.Question);

    internal static DbTable T => new("vr_t", "t");

    internal static DbTable S => new("vr_s", "s");

    /// <summary>The statement an Example runs, for one engine.</summary>
    internal static ISqlBuilder Build(EngineExample e, Dbms dbms)
    {
        DbTable t = T;
        object? v = ValueKinds.Sample(e.Kind, t);
        object[] key = [v!, t.Column("id")];

        return (e.Template, e.Context) switch
        {
            ("ORD", Context.TopLevel) => Select(t.Column("id"), t.Column("b")).From(t)
                .OrderBy(key),
            ("ORD", Context.Subquery) => Outer(Page(Select(t.Column("id"), t.Column("b")).From(t)
                .OrderBy(key), dbms)),
            ("ORD", Context.Cte) => OuterCte(Page(Select(t.Column("id"), t.Column("b")).From(t)
                .OrderBy(key), dbms)),
            ("ORD", Context.SetOperationBranch) => Select(t.Column("id"), t.Column("b")).From(t)
                .Where(t.Column("id") <= 2)
                .UnionAll
                .Select(t.Column("id"), t.Column("b")).From(t).Where(t.Column("id") > 2)
                .OrderBy(v!, 1),
            ("ORDX-WINDOW", _) => Select(t.Column("id"), RowNumber().Over(OrderBy(key)))
                .From(t).OrderBy(t.Column("id")),
            ("ORDX-AGG", _) => Select(Aggregate(dbms, t, key)).From(t),
            ("GRP", _) => IsOrdinalShaped(e.Kind)
                ? Select(Count(t.Column("id")), t.Column("b")).From(t).GroupBy(v!)
                : Select(Count(t.Column("id"))).From(t).GroupBy(v!),
            ("GEL", _) => Select(Count(t.Column("id"))).From(t).GroupBy(Rollup(v!)),
            ("PART", _) => Select(t.Column("id"), RowNumber().Over(PartitionBy(v!)
                .OrderBy(t.Column("id")))).From(t).OrderBy(t.Column("id")),
            ("DON", _) => Select(DistinctOn(v!), t.Column("id"), t.Column("g")).From(t),
            ("SEL", _) => Select(v!).From(t).Where(t.Column("id") == 1),
            ("EQ", _) => Select(t.Column("id")).From(t).Where(t.Column("b") == v!)
                .OrderBy(t.Column("id")),
            ("PLUS", _) => Select(t.Column("a") + v!).From(t).Where(t.Column("id") == 1),
            ("ROUND", _) => Select(Round(75, v!)).From(t).Where(t.Column("id") == 1),
            ("PLUSDATE", _) => Select(t.Column("d") + v!).From(t).Where(t.Column("id") == 1),
            ("MINUSDATE", _) => Select(t.Column("d") - v!).From(t).Where(t.Column("id") == 1),
            _ => throw new ArgumentOutOfRangeException(nameof(e)),
        };
    }

    internal static bool IsOrdinalShaped(ValueKind k) => k is ValueKind.IntPositive
        or ValueKind.Short or ValueKind.Byte
        or ValueKind.Long or ValueKind.Double or ValueKind.DoubleWhole or ValueKind.Decimal
        or ValueKind.BindInt
        or ValueKind.IntNegative or ValueKind.IntZero;

    private static ISqlBuilder Page(ISelectBuilderOrderBy ordered, Dbms dbms) =>
        dbms is Dbms.Oracle or Dbms.SqlServer
            ? ordered.OffsetRows(0).FetchNext(2)
            : ordered.Limit(2);

    private static ISqlBuilder Outer(ISqlBuilder inner)
    {
        SubqueryDerivedTable x = ((ISubquery)inner).AsTable("x");
        return Select(x.Column("id")).From(x).OrderBy(x.Column("id"));
    }

    private static ISqlBuilder OuterCte(ISqlBuilder inner)
    {
        Cte c = new("c");
        return With(c.As((ISubquery)inner)).Select(c.Column("id")).From(c)
            .OrderBy(c.Column("id"));
    }

    private static SqlExpression Aggregate(Dbms dbms, DbTable t, object[] key) => dbms switch
    {
        Dbms.MySql => GroupConcat(t.Column("a"), OrderBy(key)),
        Dbms.Oracle => Listagg(t.Column("a"), ",").WithinGroup(OrderBy(key)),
        Dbms.SqlServer => StringAgg(t.Column("a"), ",").WithinGroup(OrderBy(key)),
        _ => StringAgg(t.Column("a"), ",", OrderBy(key)),
    };

    /// <summary>The rows a reading returns on this data, as the runner normalizes them;
    /// null when the template gives the reading no row set (an engine error is the
    /// expectation instead, or nothing is asserted).</summary>
    internal static string[]? RowsFor(EngineExample e, string reading, Lane lane)
    {
        Dbms d = ValueReadingModel.DbmsOf(lane);
        bool strictGroup = d != Dbms.Sqlite;

        return (e.Template, e.Context, reading) switch
        {
            ("ORD", Context.TopLevel or Context.SetOperationBranch, "ordinal" or "column"
                or "expression") => Ids(5, 4, 3, 2, 1),
            ("ORD", Context.TopLevel or Context.SetOperationBranch, "constant")
                => Ids(1, 2, 3, 4, 5),
            ("ORD", _, "ordinal" or "column" or "expression") => Ids(4, 5),
            ("ORD", _, "constant") => Ids(1, 2),
            ("ORDX-WINDOW", _, "constant") => ["1|1", "2|2", "3|3", "4|4", "5|5"],
            ("ORDX-WINDOW", _, "column" or "expression") => ["1|5", "2|4", "3|3", "4|2", "5|1"],
            ("ORDX-AGG", _, "constant") => [d == Dbms.MySql ? "e,d,c,b,a" : "e,d,c,b,a"],
            ("ORDX-AGG", _, "column") => ["a,b,c,d,e"],
            ("GRP", _, "ordinal" or "column" or "expression") => IsOrdinalShaped(e.Kind)
                ? ["1|10", "1|20", "1|30", "1|40", "1|50"]
                : ["1", "1", "1", "1", "1"],
            ("GRP", _, "constant") => IsOrdinalShaped(e.Kind) && strictGroup ? null
                : IsOrdinalShaped(e.Kind) ? ["5|*"] : ["5"],
            ("GEL", _, "constant" or "subquery") => ["5", "5"],
            ("GEL", _, "column" or "expression") => ["1", "1", "1", "1", "1", "5"],
            ("PART", _, "constant" or "subquery") => ["1|1", "2|2", "3|3", "4|4", "5|5"],
            ("PART", _, "column" or "expression") => ["1|1", "2|1", "3|1", "4|1", "5|1"],
            ("DON", _, "ordinal") => ["#3"],
            ("DON", _, "constant" or "subquery") => ["#1"],
            ("DON", _, "column" or "expression") => ["#5"],
            ("SEL", _, _) => [SelectedValue(e.Kind)],
            ("EQ", _, "quantified") => ["2", "4"],
            ("EQ", _, "column") => ["1", "2", "3", "4", "5"],
            ("EQ", _, "value") => [],
            ("PLUS", _, "concatenation") => ["eb"],
            ("ROUND", _, "value") => [e.Kind == ValueKind.IntNegative ? "80" : "75"],
            ("PLUSDATE", _, "value") => ["2024-01-01T12:00:00.0000000"],
            ("MINUSDATE", _, "value") => ["2024-01-01T08:00:00.0000000"],
            ("SETSWAP", _, "column") => ["1|1|50"],
            _ => null,
        };
    }

    private static string[] Ids(params int[] ids) =>
        [.. ids.Select(i => i.ToString(CultureInfo.InvariantCulture))];

    private static string SelectedValue(ValueKind k) => k switch
    {
        ValueKind.IntZero => "0",
        ValueKind.IntPositive or ValueKind.Long or ValueKind.BindInt or ValueKind.UserEnum => "2",
        ValueKind.IntNegative => "-1",
        ValueKind.Double or ValueKind.Decimal => "2.5",
        ValueKind.DoubleWhole => "2",
        ValueKind.Text or ValueKind.Char or ValueKind.BindText => "b",
        ValueKind.Bool => "1",
        ValueKind.BindNull or ValueKind.SqlNull => "NULL",
        ValueKind.Column => "50",
        ValueKind.Expression => "51",
        ValueKind.Subquery => "40",
        _ => "~", // a driver-mapped type (Q10): any value, no error
    };

    /// <summary>The reading the model's cell promises, as a RowsFor token.</summary>
    internal static string ReadingOf(EngineExample e, Cell cell) => cell.Shape switch
    {
        Shape.Ordinal => "ordinal",
        Shape.ConstantKey => "constant",
        Shape.Literal when e.Template.StartsWith("ORD", StringComparison.Ordinal) => "constant",
        Shape.AsWritten => e.Kind == ValueKind.Expression ? "expression" : "column",
        Shape.ScalarSubquery => "subquery",
        Shape.PositionBound => "quantified",
        _ when cell.Meaning == "concatenation" => "concatenation",
        _ => "value",
    };
}

/// <summary>Runs the derived Examples on one live engine and prints one line per
/// Example: the lane, the id, the expectation, what the engine returned, a verdict.</summary>
public abstract class EngineExampleTestBase
{
    private readonly Func<IDbConnection> _open;
    private readonly Lane _lane;
    private readonly ITestOutputHelper _output;

    protected EngineExampleTestBase(Func<IDbConnection> open, Lane lane, ITestOutputHelper output)
    {
        _open = open;
        _lane = lane;
        _output = output;
    }

    [Fact]
    public void Examples_ReturnTheModelsRows()
    {
        Dbms dbms = ValueReadingModel.DbmsOf(_lane);
        using IDbConnection cnn = _open();
        Seed(cnn, dbms);

        List<string> mismatches = new();
        List<string> lines = new();

        foreach (EngineExample e in ExampleCatalog.All)
        {
            Cell cell = ValueReadingModel.Expect(e.Class, e.Kind, _lane, e.Context);
            string expected = Describe(e, cell);
            string observed = Observe(cnn, e, dbms);
            string verdict = Verdict(cell, expected, observed);

            string line = $"VR\t{_lane}\t{e.Id}\t{cell.Outcome}\t{expected}\t{observed}\t{verdict}";
            lines.Add(line);
            _output.WriteLine(line);
            if (verdict == "MISMATCH")
            {
                mismatches.Add(line);
            }
        }

        string? dir = Environment.GetEnvironmentVariable("VR_RESULTS_DIR");
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, $"engine-{_lane}.tsv"), lines);
        }

        Assert.True(mismatches.Count == 0, string.Join("\n", lines));
    }

    private string Describe(EngineExample e, Cell cell)
    {
        switch (cell.Outcome)
        {
            case Outcome.BuildRejects:
                return "BUILD-THROW";
            case Outcome.EngineRejects:
                return "ENGINE-ERROR";
            case Outcome.Executes:
                string[]? rows = ExampleCatalog.RowsFor(
                    e, ExampleCatalog.ReadingOf(e, cell), _lane);
                return rows is null ? "ENGINE-ERROR" : string.Join(",", rows);
            default:
                string[]? meant = ExampleCatalog.RowsFor(
                    e, ExampleCatalog.ReadingOf(e, cell), _lane);
                return meant is null ? "-" : "(" + string.Join(",", meant) + ")";
        }
    }

    private static string Verdict(Cell cell, string expected, string observed)
    {
        if (cell.Outcome is not (Outcome.Executes or Outcome.EngineRejects or Outcome.BuildRejects))
        {
            return "RECORDED"; // analyzer, question or delegated cells: observed only
        }

        if (expected == "ENGINE-ERROR")
        {
            return observed.StartsWith("ENGINE-ERROR", StringComparison.Ordinal)
                ? "OK"
                : "MISMATCH";
        }

        if (expected == "~")
        {
            return observed.StartsWith("ENGINE-ERROR", StringComparison.Ordinal)
                || observed == "BUILD-THROW" ? "MISMATCH" : "OK";
        }

        if (expected.Contains('*', StringComparison.Ordinal))
        {
            string prefix = expected[..expected.IndexOf('*', StringComparison.Ordinal)];
            return observed.StartsWith(prefix, StringComparison.Ordinal)
                && !observed.Contains(',', StringComparison.Ordinal) ? "OK" : "MISMATCH";
        }

        return expected == observed ? "OK" : "MISMATCH";
    }

    private static string Observe(IDbConnection cnn, EngineExample e, Dbms dbms)
    {
        if (e.Template == "SETSWAP")
        {
            return ObserveSwap(cnn);
        }

        ISqlBuilder builder;
        try
        {
            ExampleCatalog.Build(e, dbms).Build(dbms);
            builder = ExampleCatalog.Build(e, dbms); // one builder builds once (#676)
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return "BUILD-THROW";
        }

        try
        {
            List<string> rows = new();
            foreach (object row in cnn.Query(builder))
            {
                IDictionary<string, object?> map = (IDictionary<string, object?>)row;
                // The ORD templates select b only so that position 2 exists; the id
                // sequence is the reading.
                rows.Add(e.Template == "ORD"
                    ? Normalize(map.Values.First())
                    : string.Join("|", map.Values.Select(Normalize)));
            }

            if (e.Template == "DON")
            {
                return "#" + rows.Count.ToString(CultureInfo.InvariantCulture);
            }

            if (e.Template is "GRP" or "GEL")
            {
                rows.Sort(StringComparer.Ordinal);
            }

            if (e.Template == "GRP" && ExampleCatalog.IsOrdinalShaped(e.Kind) && rows.Count == 1)
            {
                return rows[0][..rows[0].IndexOf('|', StringComparison.Ordinal)] + "|*";
            }

            return string.Join(",", rows);
        }
        catch (DbException ex)
        {
            string message = ex.Message.Replace('\t', ' ').Replace('\n', ' ');
            return "ENGINE-ERROR: " + (message.Length > 120 ? message[..120] : message);
        }
        catch (Exception ex)
        {
            // The driver failing to read a value back (Oracle 21c and a bound bool).
            return $"DRIVER-ERROR: {ex.GetType().Name}";
        }
    }

    // UPDATE vr_t SET b = g, g = b WHERE id = 1, in a transaction that is rolled back.
    private static string ObserveSwap(IDbConnection cnn)
    {
        DbTable u = new("vr_t");
        using IDbTransaction tx = cnn.BeginTransaction();
        try
        {
            cnn.Execute(
                Update(u).Set(u.Column("b") == u.Column("g"), u.Column("g") == u.Column("b"))
                    .Where(u.Column("id") == 1),
                tx);
            IDictionary<string, object?> row = (IDictionary<string, object?>)cnn.Query(
                Select(u.Column("id"), u.Column("b"), u.Column("g")).From(u)
                    .Where(u.Column("id") == 1),
                tx).Single();
            return string.Join("|", row.Values.Select(Normalize));
        }
        catch (DbException ex)
        {
            return "ENGINE-ERROR: " + ex.Message.Replace('\n', ' ');
        }
        finally
        {
            tx.Rollback();
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
        DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "?",
    };

    private static void Seed(IDbConnection cnn, Dbms dbms)
    {
        string integer = dbms == Dbms.Oracle ? "NUMBER(10)" : "INT";
        string text = dbms == Dbms.Oracle ? "VARCHAR2(10)" : "VARCHAR(10)";
        string stamp = dbms switch
        {
            Dbms.SqlServer => "DATETIME2",
            Dbms.MySql => "DATETIME",
            _ => "TIMESTAMP",
        };

        foreach (string table in new[] { "vr_t", "vr_s" })
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

        DbTable t = new("vr_t");
        foreach ((int id, string a, int b, int g) in ExampleCatalog.Rows)
        {
            cnn.Execute(InsertInto(
                    t, t.Column("id"), t.Column("a"), t.Column("b"), t.Column("g"), t.Column("d"))
                .Values(id, a, b, g, new DateTime(2024, 1, 1, 10, 0, 0)));
        }

        DbTable s = new("vr_s");
        foreach ((int id, int b) in ExampleCatalog.SideRows)
        {
            cnn.Execute(InsertInto(s, s.Column("id"), s.Column("b")).Values(id, b));
        }
    }
}
