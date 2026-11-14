using System.Collections.Concurrent;
using CsCheck;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>The committed PICT models, each <c>Models/&lt;name&gt;.pict</c> beside the
/// <c>&lt;name&gt;.cases.csv</c> <c>tools/regen-pict.sh</c> generates from it, read from the
/// test output directory.</summary>
internal static class CaseModels
{
    private static readonly ConcurrentDictionary<string, PictModel> s_models = new();

    public static string ModelsDirectory =>
        Path.Combine(AppContext.BaseDirectory, "Randomized", "Models");

    public static IReadOnlyList<string> Names => [.. Directory
        .GetFiles(ModelsDirectory, "*.pict")
        .Select(path => Path.GetFileNameWithoutExtension(path))
        .Order(StringComparer.Ordinal)];

    public static TheoryData<string> TheoryNames()
    {
        TheoryData<string> data = [];
        foreach (string name in Names)
        {
            data.Add(name);
        }

        return data;
    }

    public static string ModelPath(string model) => Path.Combine(ModelsDirectory, model + ".pict");

    public static string CasesPath(string model) =>
        Path.Combine(ModelsDirectory, model + ".cases.csv");

    public static PictModel Model(string model) =>
        s_models.GetOrAdd(model, m => PictModel.Parse(File.ReadAllText(ModelPath(m))));

    /// <summary>The header and the data rows, each split at its commas; row 1 is the first
    /// line after the header.</summary>
    public static (string[] Header, List<string[]> Rows) ReadRaw(string model)
    {
        string[] lines = File.ReadAllLines(CasesPath(model));
        string[] header = lines[0].Split(',');
        List<string[]> rows = [.. lines.Skip(1).Where(l => l.Length > 0).Select(l => l.Split(','))];
        return (header, rows);
    }
}

/// <summary>One seed's statements for a CSV row, generated only when the Build gate asks, so a
/// generator that throws is reported against its row and seed.</summary>
internal sealed record BuildCase(
    Dbms Dbms,
    string Seed,
    Func<IReadOnlyList<PlannedStatement>> Statements);

/// <summary>
/// What the DB-less gates need of a model beyond its two files: the typed row its generator
/// reads (<see cref="FactorNames"/> in CSV order, <see cref="ParseRow"/> throwing on a level it
/// lacks), the matrix keys each level emits, and the statements a row builds.
/// </summary>
/// <param name="BuildCases">Every statement a CSV row's lane builds, from the row's number and
/// values; <see langword="null"/> while the model has no generator.</param>
internal sealed record CaseModelBinding(
    string[] FactorNames,
    Action<IReadOnlyList<string>> ParseRow,
    Func<string, string, IReadOnlyList<LevelKey>> LevelKeys,
    Func<int, IReadOnlyList<string>, IEnumerable<BuildCase>>? BuildCases);

/// <summary>The binding of every model; a model file without one fails
/// <c>PictModelTests</c>.</summary>
internal static class CaseModelCatalog
{
    public static readonly IReadOnlyDictionary<string, CaseModelBinding> Bindings =
        new Dictionary<string, CaseModelBinding>
        {
            [CaseTable.Model] = new(
                CaseRow.FactorNames,
                values => CaseRow.Parse(0, values),
                LevelMatrixKeys.For,
                CaseTable.BuildCases),
        };

    public static CaseModelBinding For(string model) =>
        Bindings.TryGetValue(model, out CaseModelBinding? binding)
            ? binding
            : throw new KeyNotFoundException(
                $"{model}.pict has no binding; add one to CaseModelCatalog.Bindings.");
}

/// <summary>The CsCheck seeds a lane runs for one CSV row.</summary>
internal static class CaseSeeds
{
    public const string Variable = "SQLARTISAN_RANDOMIZED_SEED";

    private const int PerRow = 3;

    public static string[] For(int rowNumber) =>
        Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } seed
            ? [seed]
            : [.. Enumerable.Range(0, PerRow)
                .Select(i => new PCG((uint)rowNumber, (ulong)i).ToString())];
}
