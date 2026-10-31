using System.Reflection;

namespace SqlArtisan.IntegrationTests.NameResolution;

// Trial (step 3): the position axis of the name-resolution model, enumerated from the
// shipped assembly — every public statement-builder stage method, i.e. every clause a
// column reference can be rendered into, plus the factories that render a column name.
public static class ClauseInventory
{
    public sealed record Clause(string Stage, string Method, string Signature);

    public static IReadOnlyList<Clause> Stages { get; } = Enumerate();

    private static List<Clause> Enumerate()
    {
        Type[] stageTypes = [.. typeof(Sql).Assembly.GetExportedTypes()
            .Where(t => t.IsInterface && t.Namespace == "SqlArtisan"
                && (t.Name.Contains("Builder", StringComparison.Ordinal)
                    || t.Name is "IPagination" or "IUpsert" or "IReturning" or "IJoinOperator"
                        or "IForUpdate" or "ISetOperator" or "ISubquery"))
            .OrderBy(t => t.Name, StringComparer.Ordinal)];

        return [.. stageTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance
                    | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.Name != "Build")
                .Select(m => new Clause(t.Name, m.Name, Sig(m))))];
    }

    private static string Sig(MethodInfo m) =>
        $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})";
}

public sealed class ClauseInventoryDumpTests
{
    [Fact]
    public void Clauses_Dump_WritesTsv()
    {
        string? path = Environment.GetEnvironmentVariable("NR_CLAUSES_TSV");
        if (path is null)
        {
            return;
        }

        File.WriteAllLines(
            path, ClauseInventory.Stages.Select(c => $"{c.Stage}\t{c.Method}\t{c.Signature}"));
    }
}
