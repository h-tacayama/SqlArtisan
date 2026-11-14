using SqlArtisan.IntegrationTests.Randomized;

namespace SqlArtisan.IntegrationTests.Tests;

/// <summary>
/// The DB-less Build gate: every statement a lane would run for each CSV row of each model with
/// a generator must build for the row's engine, so a <c>Build()</c> guard the model or generator
/// has drifted from fails the PR instead of the nightly lane.
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
}
