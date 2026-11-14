using SqlArtisan.Analyzers;
using SqlArtisan.IntegrationTests.Randomized;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>The DB-less gate between each <c>Models/*.pict</c>, its <c>.cases.csv</c> and the
/// analyzer's verdicts: a row either excludes, or a valid pair or sub-model tuple no row holds,
/// fails; a relaxed constraint opening only 3-way tuples passes.</summary>
public sealed class PictModelTests
{
    private static readonly Dbms[] s_engines =
        [Dbms.MySql, Dbms.Oracle, Dbms.PostgreSql, Dbms.Sqlite, Dbms.SqlServer];

    public static TheoryData<string> Models => CaseModels.TheoryNames();

    [Fact]
    public void ModelsDirectory_EveryModel_HasCasesCsvAndBinding()
    {
        string[] csvModels = [.. Directory
            .GetFiles(CaseModels.ModelsDirectory, "*.cases.csv")
            .Select(path => Path.GetFileName(path)[..^".cases.csv".Length])
            .Order(StringComparer.Ordinal)];

        Assert.Contains(CaseTable.Model, CaseModels.Names);
        Assert.Equal(CaseModels.Names, csvModels);
        Assert.Equal(
            CaseModels.Names,
            CaseModelCatalog.Bindings.Keys.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void CasesCsv_Header_MatchesModelFactors(string model)
    {
        PictModel pict = CaseModels.Model(model);
        CaseModelBinding binding = CaseModelCatalog.For(model);
        (string[] header, List<string[]> rows) = CaseModels.ReadRaw(model);

        Assert.Equal("Dbms", header[0]);
        Assert.Equal(pict.Factors.Select(f => f.Name), header);
        Assert.Equal(binding.FactorNames, header);
        List<string> unknown = [.. rows.SelectMany((values, i) => values
            .Select((value, f) => (value, f))
            .Where(c => c.f >= pict.Factors.Count
                || !pict.Factors[c.f].Levels.Contains(c.value))
            .Select(c => $"row {i + 1}: '{c.value}'"))];
        Assert.True(
            unknown.Count == 0,
            $"Values outside {model}.pict:\n  " + string.Join("\n  ", unknown));

        // Every level the model names must parse to a typed row the generator handles.
        string[] firstLevels = [.. pict.Factors.Select(f => f.Levels[0])];
        for (int f = 0; f < pict.Factors.Count; f++)
        {
            foreach (string level in pict.Factors[f].Levels)
            {
                string[] values = [.. firstLevels];
                values[f] = level;
                binding.ParseRow(values);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void CasesCsv_EveryRow_SatisfiesModelConstraints(string model)
    {
        List<string> violations = [];
        foreach ((Dictionary<string, string> row, int number) in Rows(model))
        {
            violations.AddRange(CaseModels.Model(model).Constraints
                .Where(c => c.Holds(row) != true)
                .Select(c => $"row {number}: {c.Text}"));
        }

        Assert.True(
            violations.Count == 0,
            $"Rows {model}.pict's constraints exclude (regenerate with tools/regen-pict.sh):\n  "
                + string.Join("\n  ", violations));
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void CasesCsv_EveryRow_IsSupportedByDialectMatrix(string model)
    {
        CaseModelBinding binding = CaseModelCatalog.For(model);
        List<string> unsupported = [];
        (string[] header, List<string[]> rows) = CaseModels.ReadRaw(model);
        for (int i = 0; i < rows.Count; i++)
        {
            Dbms dbms = Enum.Parse<Dbms>(rows[i][0]);
            for (int f = 1; f < header.Length; f++)
            {
                foreach (LevelKey key in binding.LevelKeys(header[f], rows[i][f]))
                {
                    if (MatrixLookup.Verdict(dbms, key) is { } verdict)
                    {
                        unsupported.Add($"row {i + 1} {header[f]}={rows[i][f]}: {verdict}");
                    }
                }
            }

            Dictionary<string, string> row =
                header.Zip(rows[i]).ToDictionary(p => p.First, p => p.Second);
            foreach (ComboKey combo in binding.ComboKeys.Where(c => c.AppliesTo(row)))
            {
                foreach (LevelKey key in combo.Keys)
                {
                    if (MatrixLookup.Verdict(dbms, key) is { } verdict)
                    {
                        unsupported.Add($"row {i + 1} {combo}: {verdict}");
                    }
                }
            }
        }

        Assert.True(
            unsupported.Count == 0,
            $"Rows the analyzer says their engine cannot run (a {model}.pict constraint has "
                + "drifted from it):\n  " + string.Join("\n  ", unsupported));
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void Model_EveryMatrixSupportedLevel_IsAllowedPerDbms(string model)
    {
        PictModel pict = CaseModels.Model(model);
        CaseModelBinding binding = CaseModelCatalog.For(model);
        string[] engines = pict.Factors[0].Levels;
        List<string> excluded = [];
        foreach ((string factor, string[] levels) in pict.Factors.Skip(1))
        {
            foreach (string level in levels)
            {
                foreach (string engine in engines.Where(e => MatrixLookup.SupportsAll(
                    Enum.Parse<Dbms>(e),
                    binding.LevelKeys(factor, level))))
                {
                    Dictionary<string, string> pair = new() { ["Dbms"] = engine, [factor] = level };
                    if (!pict.IsSatisfiable(pair))
                    {
                        excluded.Add($"{factor}={level} on {engine}");
                    }
                }
            }
        }

        // A combination's keys hold on fewer engines than its levels' own, so each engine its
        // keys and its levels' keys allow must still reach every tuple it names.
        foreach (ComboKey combo in binding.ComboKeys)
        {
            IEnumerable<Dictionary<string, string>> tuples = [[]];
            foreach ((string factor, string[] levels) in combo.Levels)
            {
                tuples = [.. tuples.SelectMany(t => levels.Select(l =>
                    new Dictionary<string, string>(t) { [factor] = l }))];
            }

            foreach (Dictionary<string, string> tuple in tuples)
            {
                foreach (string engine in engines.Where(e => MatrixLookup.SupportsAll(
                    Enum.Parse<Dbms>(e),
                    [
                        .. combo.Keys,
                        .. tuple.SelectMany(kv => binding.LevelKeys(kv.Key, kv.Value)),
                    ])))
                {
                    Dictionary<string, string> cell = new(tuple) { ["Dbms"] = engine };
                    if (!pict.IsSatisfiable(cell))
                    {
                        string levels =
                            string.Join(", ", tuple.Select(kv => $"{kv.Key}={kv.Value}"));
                        excluded.Add($"{levels} on {engine}");
                    }
                }
            }
        }

        Assert.True(
            excluded.Count == 0,
            $"Levels the analyzer supports but {model}.pict never generates (an over-tight "
                + "constraint):\n  " + string.Join("\n  ", excluded));
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void CasesCsv_EveryValidPair_IsCovered(string model)
    {
        PictModel pict = CaseModels.Model(model);
        List<string> missing = [];
        for (int a = 0; a < pict.Factors.Count; a++)
        {
            for (int b = a + 1; b < pict.Factors.Count; b++)
            {
                string[] pair = [pict.Factors[a].Name, pict.Factors[b].Name];
                missing.AddRange(MissingTuples(model, pair));
            }
        }

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} valid pairs appear in no row:\n  " + string.Join("\n  ", missing));
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void CasesCsv_EverySubModelTuple_IsCovered(string model)
    {
        // A sub-model line the parser dropped would check no tuple and pass.
        Assert.Equal(
            File.ReadLines(CaseModels.ModelPath(model)).Count(l => l.TrimStart().StartsWith('{')),
            CaseModels.Model(model).SubModels.Count);
        List<string> missing = [];
        foreach ((string[] factors, int order) in CaseModels.Model(model).SubModels)
        {
            Assert.Equal(factors.Length, order);
            missing.AddRange(MissingTuples(model, factors));
        }

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} valid sub-model tuples appear in no row:\n  "
                + string.Join("\n  ", missing));
    }

    [Fact]
    public void Verdict_IntoByArity_TellsOutputIntoFromReturningInto()
    {
        Assert.Null(MatrixLookup.Verdict(Dbms.SqlServer, ("Into", 2)));
        Assert.NotNull(MatrixLookup.Verdict(Dbms.Oracle, ("Into", 2)));
        Assert.Null(MatrixLookup.Verdict(Dbms.Oracle, "Into"));
        Assert.NotNull(MatrixLookup.Verdict(Dbms.SqlServer, "Into"));
    }

    [Fact]
    public void Verdict_WhenMatchedWithCondition_IsUnsupportedOnOracleOnly()
    {
        Assert.Null(MatrixLookup.Verdict(Dbms.Oracle, "WhenMatched"));
        Assert.NotNull(MatrixLookup.Verdict(Dbms.Oracle, ("WhenMatched", 1)));
        Assert.Null(MatrixLookup.Verdict(Dbms.PostgreSql, ("WhenMatched", 1)));
        Assert.Null(MatrixLookup.Verdict(Dbms.SqlServer, ("WhenMatched", 1)));
    }

    [Fact]
    public void Verdict_ArityWithoutRow_FallsBackToMemberRow()
    {
        Assert.Null(MatrixLookup.Verdict(Dbms.MySql, ("Upper", 1)));
        Assert.Equal(
            "DialectMatrix marks NullsFirst/1 unsupported on MySql",
            MatrixLookup.Verdict(Dbms.MySql, ("NullsFirst", 1)));
    }

    [Fact]
    public void Verdict_JoinedUpdateFrom_ReadsAnalyzerShapeAndFloor()
    {
        Dbms[] supported =
        [
            .. s_engines.Where(d =>
                MatrixLookup.Supports(d, ContextRules.DmlShape.JoinedUpdateFrom)),
        ];

        // SQLite takes UPDATE ... FROM from 3.33, below its 3.50 baseline; Oracle from 23.
        Assert.Equal([Dbms.PostgreSql, Dbms.Sqlite, Dbms.SqlServer], supported);
        Assert.Equal(
            "JoinedUpdateFrom needs Oracle 23, above the 21.3 baseline",
            MatrixLookup.Verdict(Dbms.Oracle, ContextRules.DmlShape.JoinedUpdateFrom));
    }

    [Fact]
    public void Verdict_DeleteUsing_ReadsTheBuildGuardOnSqlServer()
    {
        IReadOnlyList<LevelKey> keys = CaseModelCatalog.For("delete").LevelKeys("Join", "Using");
        Dbms[] supported = [.. s_engines.Where(d => MatrixLookup.SupportsAll(d, keys))];

        // The analyzer reports DELETE ... USING on Oracle alone; SQL Server's is Build()'s.
        Assert.Equal([Dbms.PostgreSql], supported);
        Assert.Equal(
            "Build() rejects it on SqlServer (DmlTargetGuard.ThrowIfSqlServerDeleteUsing)",
            MatrixLookup.Verdict(Dbms.SqlServer, BuildGuards.DeleteUsingOnSqlServer));
    }

    // The guard reads the target and the action together, so no one level of either carries it.
    [Fact]
    public void ComboKey_TargetlessDoUpdate_ReadsTheBuildGuardOnPostgreSqlOnly()
    {
        ComboKey combo = Assert.Single(CaseModelCatalog.For("upsert").ComboKeys);
        Dbms[] supported = [.. s_engines.Where(d => MatrixLookup.SupportsAll(d, combo.Keys))];

        Assert.True(combo.AppliesTo(Row("Targetless", "DoUpdateWhere")));
        Assert.False(combo.AppliesTo(Row("Targetless", "DoNothing")));
        Assert.False(combo.AppliesTo(Row("Id", "DoUpdate")));
        Assert.Equal([Dbms.MySql, Dbms.Oracle, Dbms.Sqlite, Dbms.SqlServer], supported);

        static Dictionary<string, string> Row(string arbiter, string action) =>
            new() { ["Arbiter"] = arbiter, ["Action"] = action };
    }

    // A probe off its key's dialects may still stop at another listed guard of that dialect,
    // as a leading-WITH INSERT stops at Oracle's while its key claims MySQL's.
    [Fact]
    public void BuildGuards_EveryProbe_ThrowsItsMessageOnExactlyItsDialects()
    {
        List<string> wrong = [];
        foreach (GuardKey key in BuildGuards.All)
        {
            for (int p = 0; p < key.Probes.Length; p++)
            {
                foreach (Dbms dbms in s_engines)
                {
                    string? message = null;
                    try
                    {
                        key.Probes[p]().Build(dbms);
                    }
                    catch (ArgumentException ex)
                    {
                        message = ex.Message;
                    }

                    bool holds = key.RejectedOn.Contains(dbms)
                        ? message == key.Message
                        : message is null || BuildGuards.All.Any(other => other != key
                            && other.RejectedOn.Contains(dbms) && other.Message == message);
                    if (!holds)
                    {
                        wrong.Add($"{key.Guard} probe {p} on {dbms}: {message ?? "built"}");
                    }
                }
            }
        }

        Assert.True(
            wrong.Count == 0,
            "Build() verdicts that differ from their GuardKey:\n  " + string.Join("\n  ", wrong));
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void Model_EveryGuardKey_IsProvedByBuildGuards(string model)
    {
        PictModel pict = CaseModels.Model(model);
        CaseModelBinding binding = CaseModelCatalog.For(model);
        List<string> unproved = [.. pict.Factors
            .SelectMany(f => f.Levels.SelectMany(l => binding.LevelKeys(f.Name, l)))
            .Concat(binding.ComboKeys.SelectMany(c => c.Keys))
            .OfType<GuardKey>()
            .Where(key => !BuildGuards.All.Contains(key))
            .Select(key => key.Guard)];

        Assert.True(
            unproved.Count == 0,
            "GuardKeys outside BuildGuards.All, which no probe proves:\n  "
                + string.Join("\n  ", unproved));
    }

    // A tuple is valid when some full row the constraints allow holds it; PICT promises each
    // valid one a row, which this re-derives without PICT.
    private static IEnumerable<string> MissingTuples(string model, string[] factors)
    {
        PictModel pict = CaseModels.Model(model);
        List<Dictionary<string, string>> rows = [.. Rows(model).Select(r => r.Row)];
        IEnumerable<Dictionary<string, string>> tuples = [[]];
        foreach (string factor in factors)
        {
            string[] levels = pict.Factors.Single(f => f.Name == factor).Levels;
            tuples = [.. tuples.SelectMany(t => levels.Select(l =>
                new Dictionary<string, string>(t) { [factor] = l }))];
        }

        return tuples
            .Where(t => pict.IsSatisfiable(t))
            .Where(t => !rows.Any(r => t.All(kv => r[kv.Key] == kv.Value)))
            .Select(t => string.Join(", ", t.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    private static IEnumerable<(Dictionary<string, string> Row, int Number)> Rows(string model)
    {
        (string[] header, List<string[]> rows) = CaseModels.ReadRaw(model);
        return rows.Select((values, i) => (
            header.Zip(values).ToDictionary(p => p.First, p => p.Second),
            i + 1));
    }
}
