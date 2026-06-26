using LinqToDB.Data;
using SqlArtisan.Benchmark.EfCoreModel;

namespace SqlArtisan.Benchmark;

// One-time equivalence check, run *outside* the measured loop (via `dotnet run -- validate`).
// It prints each entrant's parameter count and SQL, and asserts every entrant that must
// parameterize the query (the baseline and all builders) produced exactly two bind
// parameters. SQL text is not required to be byte-identical — dialects, alias generation,
// and EF Core's pipeline all differ — only the logical query and the parameter count must
// match. EF Core is reported for reference only and is not asserted.
public static class BenchmarkValidation
{
    public static int Run()
    {
        const int expectedParameters = 2;

        using DataConnection linq2db = Linq2dbBenchmark.CreateConnection();
        using BenchmarkDbContext efCore = EfCoreBenchmark.CreateContext();

        (string Name, Func<(string Sql, int ParameterCount)> Build, bool MustParameterize)[] entrants =
        [
            ("StringBuilder", StringBuilderBenchmark.Run, true),
            ("DapperSqlBuilder", DapperSqlBuilderBenchmark.Run, true),
            ("InterpolatedSql", InterpolatedSqlBenchmark.Run, true),
            ("linq2db", () => Linq2dbBenchmark.Run(linq2db), true),
            ("Sqlify", SqlifyBenchmark.Run, true),
            ("SqlKata", SqlKataBenchmark.Run, true),
            ("SqlArtisan", SqlArtisanBenchmark.Run, true),
            ("SqlArtisan+Dapper", SqlArtisanDapperBenchmark.Run, true),
            ("EF Core (reference)", () => EfCoreBenchmark.Run(efCore), false),
        ];

        bool ok = true;
        foreach ((string name, Func<(string Sql, int ParameterCount)> build, bool mustParameterize) in entrants)
        {
            (string sql, int parameterCount) = build();
            bool entrantOk = !mustParameterize || parameterCount == expectedParameters;
            ok &= entrantOk;

            Console.WriteLine($"=== {name} === parameters: {parameterCount}{(entrantOk ? "" : $"  <-- EXPECTED {expectedParameters}")}");
            Console.WriteLine(sql);
            Console.WriteLine();
        }

        if (ok)
        {
            Console.WriteLine($"OK: every parameterizing entrant produced {expectedParameters} parameters.");
            return 0;
        }

        Console.WriteLine("FAIL: an entrant did not parameterize the query as expected.");
        return 1;
    }
}
