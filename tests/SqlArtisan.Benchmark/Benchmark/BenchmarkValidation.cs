using LinqToDB.Data;
using SqlArtisan.Benchmark.EfCoreModel;

namespace SqlArtisan.Benchmark;

// Runs outside the measured loop (`dotnet run -- validate`). Only the parameter count is
// asserted — SQL text differs legitimately by dialect and alias generation, so the logical
// query is merely printed, which is how the SqlKata drift (#382) got through.
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
