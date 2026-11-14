using CsCheck;
using SqlArtisan.IntegrationTests.Randomized;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// The DB-less gate over every statement a lane would run: each must build for its row's engine,
/// and each seeded DML statement must be one <see cref="DmlReference"/> evaluates, so a drifted
/// guard or a statement the reference cannot check fails the PR instead of the nightly lane.
/// </summary>
public sealed class CombinatorialBuildTests
{
    public static TheoryData<string> Models => CaseModels.TheoryNames();

    [Theory]
    [MemberData(nameof(Models))]
    public void CasesCsv_EveryRowStatement_Builds(string model)
    {
        if (CaseModelCatalog.For(model).BuildCases is not { } buildCases)
        {
            return;
        }

        List<string[]> rows = CaseModels.ReadRaw(model).Rows;
        List<string> failures = [];
        HashSet<int> failedRows = [];
        int built = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            foreach (BuildCase buildCase in buildCases(i + 1, rows[i]))
            {
                string origin = $"{model}.cases.csv row {i + 1} ({string.Join(",", rows[i])}), "
                    + $"CsCheck seed {buildCase.Seed}";
                IReadOnlyList<PlannedStatement> statements;
                try
                {
                    statements = buildCase.Statements();
                }
                catch (Exception ex)
                {
                    failures.Add($"{origin}: the generator threw\n  {ex.GetType().Name}: "
                        + ex.Message);
                    failedRows.Add(i + 1);
                    continue;
                }

                foreach (PlannedStatement statement in statements)
                {
                    try
                    {
                        statement.Builder().Build(buildCase.Dbms);
                        built++;
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{origin}, {statement.Label} on {buildCase.Dbms}\n"
                            + $"  C#: {statement.CSharp.Replace("\n", "\n      ")}\n"
                            + $"  {ex.GetType().Name}: {ex.Message}");
                        failedRows.Add(i + 1);
                    }
                }
            }
        }

        Assert.True(
            failures.Count == 0,
            $"{failures.Count} of {built + failures.Count} statements from {failedRows.Count} "
                + $"rows fail Build() (reproduce with {CaseSeeds.Variable}=<seed>); the first "
                + $"{Math.Min(failures.Count, 10)}:\n" + string.Join("\n", failures.Take(10)));
        Assert.True(built > 0, $"{model} built no statement.");
    }

    // The DML lanes fail a statement DmlReference cannot evaluate; this catches a generator
    // change that brings one back on the PR, before any lane runs it.
    [Fact]
    public void DmlCasesCsv_EverySeedStatement_IsEvaluatedByTheReference()
    {
        List<string> failures = [];
        int evaluated = 0;
        foreach (DmlCaseRow row in DmlCaseTable.Rows)
        {
            Gen<DmlSpec> gen = DmlGenerators.For(row);
            foreach (string seed in CaseSeeds.For(row.Number))
            {
                DmlSpec spec = gen.Generate(PCG.Parse(seed), null!, out _);
                try
                {
                    DmlReference.Evaluate(spec);
                    evaluated++;
                }
                catch (ReferenceUnavailableException ex)
                {
                    failures.Add($"{row.Model}.cases.csv row {row.Number} ({row.Summary}), "
                        + $"CsCheck seed {seed}: {ex.Message}\n  C#: {spec.ToCSharp()}");
                }
            }
        }

        Assert.True(
            failures.Count == 0,
            $"{failures.Count} DML statements the reference cannot evaluate; the first "
                + $"{Math.Min(failures.Count, 10)}:\n" + string.Join("\n", failures.Take(10)));
        Assert.True(evaluated > 0, "No DML statement was evaluated.");
    }
}
