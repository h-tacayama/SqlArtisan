using System.Text;

namespace SqlArtisan.IntegrationTests.ValueReading;

/// <summary>
/// The DB-less half of the trial: the classification gate (every enumerated slot has a
/// class) and the build-level Examples (every slot x every value kind x every dialect,
/// observed and compared with the model's expectation). No Engine trait, so no lane
/// runs it; the CI-style filter is FullyQualifiedName~ValueReading.
/// </summary>
public sealed class ValueReadingModelTests
{
    private static readonly Dbms[] Dialects =
        [Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite, Dbms.SqlServer];

    [Fact]
    public void Inventory_EverySlot_IsClassified()
    {
        string[] unclassified = [.. PositionInventory.Slots
            .Where(s => ValueReadingModel.ClassOf(s) is null)
            .Select(s => $"{s.DeclaringType}.{s.Signature} :: {s.Parameter}")];

        Assert.True(unclassified.Length == 0, string.Join("\n", unclassified));
    }

    [Fact]
    public void BuildLevelExamples_MatchTheModel()
    {
        List<string> mismatches = new();
        StringBuilder log = new();

        foreach (PositionInventory.Slot slot in PositionInventory.Slots)
        {
            PositionClass c = ValueReadingModel.ClassOf(slot)!.Value;

            foreach (ValueKind kind in ValueKinds.All)
            {
                foreach (Dbms dbms in Dialects)
                {
                    Lane lane = LaneOf(dbms);
                    Cell cell = ValueReadingModel.Expect(c, kind, lane);
                    PositionProbe.Result observed = PositionProbe.Run(slot, kind, dbms);
                    string verdict = Judge(cell, kind, observed.Observed);

                    log.Append(slot.DeclaringType).Append('\t').Append(slot.Signature)
                        .Append('\t').Append(slot.Parameter).Append('\t').Append(c)
                        .Append('\t').Append(kind).Append('\t').Append(dbms)
                        .Append('\t').Append(cell.Outcome).Append('\t').Append(cell.Shape)
                        .Append('\t').Append(observed.Observed).Append('\t').Append(verdict)
                        .Append('\t').Append(observed.Detail.Replace('\t', ' ').Replace('\n', ' '))
                        .Append('\n');

                    if (verdict == "MISMATCH")
                    {
                        mismatches.Add($"{slot.DeclaringType}.{slot.Signature} :: {slot.Parameter}"
                            + $" [{c}] {kind} {dbms}: expected {cell.Outcome}/{cell.Shape}, "
                            + $"observed {observed.Observed} — {observed.Detail}");
                    }
                }
            }
        }

        string? path = Environment.GetEnvironmentVariable("VR_PROBE_TSV");
        if (path is not null)
        {
            File.WriteAllText(path, log.ToString());
        }

        Assert.True(
            mismatches.Count == 0,
            $"{mismatches.Count} mismatch(es):\n" + string.Join("\n", mismatches.Take(400)));
    }

    internal static Lane LaneOf(Dbms dbms) => dbms switch
    {
        Dbms.MySql => Lane.MySql,
        Dbms.Oracle => Lane.Oracle,
        Dbms.PostgreSql => Lane.PostgreSql,
        Dbms.Sqlite => Lane.Sqlite,
        _ => Lane.SqlServer,
    };

    /// <summary>"OK", "MISMATCH", or "UNCHECKED" (questions, delegated and non-SQL
    /// cells, and probes the generic invoker cannot reach).</summary>
    internal static string Judge(Cell cell, ValueKind kind, Observed observed)
    {
        if (observed == Observed.Unreachable)
        {
            return "UNREACHED";
        }

        switch (cell.Outcome)
        {
            case Outcome.CompileStops:
                return observed == Observed.CompileStop ? "OK" : "MISMATCH";
            case Outcome.BuildRejects:
                return observed is Observed.EagerThrow or Observed.BuildThrow ? "OK" : "MISMATCH";
            case Outcome.Executes:
            case Outcome.AnalyzerWarns:
            case Outcome.EngineRejects:
                if (observed is not (Observed.BuiltMarker or Observed.BuiltLiteral
                    or Observed.BuiltOther))
                {
                    return "MISMATCH";
                }

                Observed? wanted = cell.Shape switch
                {
                    Shape.Marker => Observed.BuiltMarker,
                    Shape.Literal or Shape.Ordinal => kind is ValueKind.SqlNull
                        ? Observed.BuiltOther : Observed.BuiltLiteral,
                    Shape.ConstantKey => kind is ValueKind.BindInt or ValueKind.BindText
                        or ValueKind.BindNull
                        ? Observed.BuiltMarker
                        : kind is ValueKind.SqlNull ? Observed.BuiltOther : Observed.BuiltLiteral,
                    _ => null,
                };

                return wanted is null || wanted == observed ? "OK" : "MISMATCH";
            default:
                return "UNCHECKED";
        }
    }
}
