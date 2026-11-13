using SqlArtisan.IntegrationTests.Randomized;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>The DB-less gate between <c>select.pict</c>, <c>select.cases.csv</c> and
/// <see cref="Analyzers.DialectMatrix"/>: CI never runs PICT, so a model edited without
/// <c>tools/regen-pict.sh</c>, or a constraint drifting from the matrix, fails here.</summary>
public sealed class PictModelTests
{
    private static readonly PictModel s_model =
        PictModel.Parse(File.ReadAllText(CaseTable.ModelPath));

    [Fact]
    public void CasesCsv_Header_MatchesModelFactors()
    {
        (string[] header, List<string[]> rows) = CaseTable.ReadRaw();

        Assert.Equal(s_model.Factors.Select(f => f.Name), header);
        Assert.Equal(CaseRow.FactorNames, header);
        List<string> unknown = [.. rows.SelectMany((values, i) => values
            .Select((value, f) => (value, f))
            .Where(c => c.f >= s_model.Factors.Count
                || !s_model.Factors[c.f].Levels.Contains(c.value))
            .Select(c => $"row {i + 1}: '{c.value}'"))];
        Assert.True(
            unknown.Count == 0,
            "Values outside the model:\n  " + string.Join("\n  ", unknown));

        // Every level the model names must parse to a CaseRow value the generator handles.
        string[] firstLevels = [.. s_model.Factors.Select(f => f.Levels[0])];
        for (int f = 0; f < s_model.Factors.Count; f++)
        {
            foreach (string level in s_model.Factors[f].Levels)
            {
                string[] values = [.. firstLevels];
                values[f] = level;
                CaseRow.Parse(0, values);
            }
        }
    }

    [Fact]
    public void CasesCsv_EveryRow_SatisfiesModelConstraints()
    {
        List<string> violations = [];
        foreach ((Dictionary<string, string> row, int number) in Rows())
        {
            violations.AddRange(s_model.Constraints
                .Where(c => c.Holds(row) != true)
                .Select(c => $"row {number}: {c.Text}"));
        }

        Assert.True(
            violations.Count == 0,
            "Rows the model's constraints exclude (regenerate with tools/regen-pict.sh):\n  "
                + string.Join("\n  ", violations));
    }

    [Fact]
    public void CasesCsv_EveryRow_IsSupportedByDialectMatrix()
    {
        List<string> unsupported = [];
        (string[] header, List<string[]> rows) = CaseTable.ReadRaw();
        for (int i = 0; i < rows.Count; i++)
        {
            Dbms dbms = Enum.Parse<Dbms>(rows[i][0]);
            for (int f = 1; f < header.Length; f++)
            {
                foreach (string key in LevelMatrixKeys.For(header[f], rows[i][f]))
                {
                    if (MatrixLookup.Verdict(dbms, key) is { } verdict)
                    {
                        unsupported.Add($"row {i + 1} {header[f]}={rows[i][f]}: {verdict}");
                    }
                }
            }
        }

        Assert.True(
            unsupported.Count == 0,
            "Rows the matrix says their engine cannot run (a select.pict constraint has drifted "
                + "from DialectMatrix):\n  " + string.Join("\n  ", unsupported));
    }

    [Fact]
    public void Model_EveryMatrixSupportedLevel_IsAllowedPerDbms()
    {
        string[] engines = s_model.Factors[0].Levels;
        List<string> excluded = [];
        foreach ((string factor, string[] levels) in s_model.Factors.Skip(1))
        {
            foreach (string level in levels)
            {
                foreach (string engine in engines.Where(e =>
                    MatrixLookup.SupportsLevel(Enum.Parse<Dbms>(e), factor, level)))
                {
                    Dictionary<string, string> pair = new() { ["Dbms"] = engine, [factor] = level };
                    if (!s_model.IsSatisfiable(pair))
                    {
                        excluded.Add($"{factor}={level} on {engine}");
                    }
                }
            }
        }

        Assert.True(
            excluded.Count == 0,
            "Levels the matrix supports but the model never generates (an over-tight "
                + "constraint):\n  " + string.Join("\n  ", excluded));
    }

    [Fact]
    public void CasesCsv_EveryValidPair_IsCovered()
    {
        List<string> missing = [];
        for (int a = 0; a < s_model.Factors.Count; a++)
        {
            for (int b = a + 1; b < s_model.Factors.Count; b++)
            {
                missing.AddRange(MissingTuples([s_model.Factors[a].Name, s_model.Factors[b].Name]));
            }
        }

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} valid pairs appear in no row:\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void CasesCsv_EverySubModelTuple_IsCovered()
    {
        Assert.NotEmpty(s_model.SubModels);
        List<string> missing = [];
        foreach ((string[] factors, int order) in s_model.SubModels)
        {
            Assert.Equal(factors.Length, order);
            missing.AddRange(MissingTuples(factors));
        }

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} valid sub-model tuples appear in no row:\n  "
                + string.Join("\n  ", missing));
    }

    // A tuple is valid when some full row the constraints allow holds it; PICT promises each
    // valid one a row, which this re-derives without PICT.
    private static IEnumerable<string> MissingTuples(string[] factors)
    {
        List<Dictionary<string, string>> rows = [.. Rows().Select(r => r.Row)];
        IEnumerable<Dictionary<string, string>> tuples = [[]];
        foreach (string factor in factors)
        {
            string[] levels = s_model.Factors.Single(f => f.Name == factor).Levels;
            tuples = [.. tuples.SelectMany(t => levels.Select(l =>
                new Dictionary<string, string>(t) { [factor] = l }))];
        }

        return tuples
            .Where(t => s_model.IsSatisfiable(t))
            .Where(t => !rows.Any(r => t.All(kv => r[kv.Key] == kv.Value)))
            .Select(t => string.Join(", ", t.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    private static IEnumerable<(Dictionary<string, string> Row, int Number)> Rows()
    {
        (string[] header, List<string[]> rows) = CaseTable.ReadRaw();
        return rows.Select((values, i) => (
            header.Zip(values).ToDictionary(p => p.First, p => p.Second),
            i + 1));
    }
}
