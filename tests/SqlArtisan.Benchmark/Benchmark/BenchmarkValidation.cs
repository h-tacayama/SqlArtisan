using System.Text.RegularExpressions;
using LinqToDB.Data;
using SqlArtisan.Benchmark.EfCoreModel;

namespace SqlArtisan.Benchmark;

// Runs outside the measured loop (`dotnet run -- validate`). Asserts the shape every
// comparison entrant must share rather than the SQL text, which differs legitimately by
// dialect and alias generation — a parameter-count-only check let #382 through.
public static class BenchmarkValidation
{
    private static readonly Regex AggregateCall = new(@"\bCOUNT\s*\(", RegexOptions.IgnoreCase);

    // Each dialect spells the keys differently, so only their number is comparable.
    private static readonly Regex GroupByKeys = new(
        @"GROUP BY (?<keys>.*?)(?: ORDER BY | HAVING |$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    // linq2db pretty-prints across lines, so the clause boundaries only line up once
    // every run of whitespace is one space.
    private static readonly Regex Whitespace = new(@"\s+");

    // #382 hid an expression inside a quoted identifier — "COUNT(orders"."id)" — which
    // reads as both an aggregate and two GROUP BY keys until the quotes are collapsed.
    private static readonly Regex QuotedIdentifier = new("\"[^\"]*\"");

    public static int Run()
    {
        const int expectedParameters = 2;
        const int expectedGroupByKeys = 2;

        using DataConnection linq2db = Linq2dbBenchmark.CreateConnection();
        using BenchmarkDbContext efCore = EfCoreBenchmark.CreateContext();

        (string Name, Func<(string Sql, int ParameterCount)> Build, bool InComparison)[] entrants =
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
        foreach ((string name, Func<(string Sql, int ParameterCount)> build, bool inComparison) in entrants)
        {
            (string sql, int parameterCount) = build();
            string? drift = inComparison
                ? Drift(sql, parameterCount, expectedParameters, expectedGroupByKeys)
                : null;
            ok &= drift is null;

            Console.WriteLine($"=== {name} === parameters: {parameterCount}{(drift is null ? "" : $"  <-- {drift}")}");
            Console.WriteLine(sql);
            Console.WriteLine();
        }

        if (ok)
        {
            Console.WriteLine(
                $"OK: every comparison entrant built the shared query with {expectedParameters} bind parameters.");
            return 0;
        }

        Console.WriteLine("FAIL: an entrant did not build the shared query as expected.");
        return 1;
    }

    private static string? Drift(string sql, int parameters, int expectedParameters, int expectedKeys)
    {
        if (parameters != expectedParameters)
        {
            return $"EXPECTED {expectedParameters} PARAMETERS";
        }

        string bare = QuotedIdentifier.Replace(Whitespace.Replace(sql, " "), "_");

        if (!AggregateCall.IsMatch(bare))
        {
            return "NO AGGREGATE — not the shared query";
        }

        Match groupBy = GroupByKeys.Match(bare);
        int keys = groupBy.Success ? groupBy.Groups["keys"].Value.Split(',').Length : 0;

        return keys == expectedKeys ? null : $"GROUP BY NAMES {keys} KEYS, EXPECTED {expectedKeys}";
    }
}
