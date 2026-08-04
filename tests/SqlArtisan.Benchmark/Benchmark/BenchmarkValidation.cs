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

    // Each dialect spells the keys differently, so only their number is comparable —
    // counted by comma, which is exact while every entrant groups by bare columns.
    private static readonly Regex GroupByKeys = new(
        @"GROUP BY (?<keys>.*?)(?: ORDER BY | HAVING |$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    // linq2db pretty-prints across lines, so the clause boundaries only line up once
    // every run of whitespace is one space.
    private static readonly Regex Whitespace = new(@"\s+");

    // #382 hid an expression inside a quoted identifier — "COUNT(orders"."id)" — which
    // reads as both an aggregate and two GROUP BY keys until the quotes are collapsed.
    private static readonly Regex QuotedIdentifier = new("\"[^\"]*\"");

    // \b fails to match a keyword fused onto an adjacent identifier ("u.idWHERE"), so a
    // template that drops its separator (as the Dapper SqlBuilder template's bare
    // Append/AppendLine calls can) fails this match instead of passing silently.
    private static readonly Regex ClauseBoundaries = new(
        @"\bSELECT\b.*\bFROM\b.*\bJOIN\b.*\bWHERE\b.*\bGROUP BY\b.*\bORDER BY\b",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static int Run()
    {
        const int expectedParameters = 2;
        const int expectedGroupByKeys = 2;

        using DataConnection linq2db = Linq2dbBenchmark.CreateConnection();
        using BenchmarkDbContext efCore = EfCoreBenchmark.CreateContext();

        (string Name, Func<(string Sql, int ParameterCount)> Build, bool BuildsSharedQuery)[] entrants =
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
        foreach ((string name, Func<(string Sql, int ParameterCount)> build, bool shared) in entrants)
        {
            (string sql, int parameterCount) = build();
            string? drift = shared
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
                $"OK: every checked entrant built the shared query with {expectedParameters} bind parameters.");
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

        if (!ClauseBoundaries.IsMatch(bare))
        {
            return "CLAUSE KEYWORDS MISSING OR OUT OF ORDER — a template may have glued two clauses together";
        }

        // Sqlify sorts by COUNT(*), so searching the whole statement would let an
        // aggregate in ORDER BY vouch for a projection that is no longer there.
        int sort = bare.LastIndexOf(" ORDER BY ", StringComparison.OrdinalIgnoreCase);

        if (!AggregateCall.IsMatch(sort < 0 ? bare : bare[..sort]))
        {
            return "NO AGGREGATE — not the shared query";
        }

        Match groupBy = GroupByKeys.Match(bare);
        int keys = groupBy.Success ? groupBy.Groups["keys"].Value.Split(',').Length : 0;

        return keys == expectedKeys
            ? null
            : $"GROUP BY NAMES {keys} {(keys == 1 ? "KEY" : "KEYS")}, EXPECTED {expectedKeys}";
    }
}
