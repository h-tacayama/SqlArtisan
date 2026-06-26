using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using LinqToDB.Data;
using SqlArtisan.Benchmark.EfCoreModel;

namespace SqlArtisan.Benchmark;

// Every entrant builds the SQL string *and* its bind-parameter collection for the same
// logical query (an INNER JOIN + GROUP BY aggregate filtered by two date parameters), so
// the comparison is apples-to-apples. Each [Benchmark] returns the produced
// (Sql, ParameterCount) tuple so BenchmarkDotNet consumes both outputs and cannot
// dead-code-eliminate the work.
//
// Builder libraries sit in the "Builders" category. The hand-written StringBuilder is a
// labeled "Baseline" floor and EF Core a labeled "ORM reference"; both do materially
// different work, so they are kept out of the builder comparison. linq2db and EF Core
// cache compiled queries and reuse a long-lived connection/context created once in
// [GlobalSetup], so the loop measures realistic warm steady-state.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class SqlBuilderBenchmarks
{
    private const string Baseline = "Baseline";
    private const string Builders = "Builders";
    private const string OrmReference = "ORM reference";

    private DataConnection _linq2db = null!;
    private BenchmarkDbContext _efCore = null!;

    [GlobalSetup]
    public void Setup()
    {
        _linq2db = Linq2dbBenchmark.CreateConnection();
        _efCore = EfCoreBenchmark.CreateContext();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _linq2db.Dispose();
        _efCore.Dispose();
    }

    // The hand-written StringBuilder + Dapper DynamicParameters is the raw floor (no
    // type safety, composition, or dialect handling), so it is a labeled baseline
    // rather than a builder entrant.
    [Benchmark]
    [BenchmarkCategory(Baseline)]
    public (string Sql, int ParameterCount) StringBuilder_DapperDynamicParams() =>
        StringBuilderBenchmark.Run();

    [Benchmark]
    [BenchmarkCategory(Builders)]
    public (string Sql, int ParameterCount) DapperSqlBuilder_DapperDynamicParams() =>
        DapperSqlBuilderBenchmark.Run();

    [Benchmark]
    [BenchmarkCategory(Builders)]
    public (string Sql, int ParameterCount) InterpolatedSql_SpecificParams() =>
        InterpolatedSqlBenchmark.Run();

    [Benchmark]
    [BenchmarkCategory(Builders)]
    public (string Sql, int ParameterCount) Linq2db_TypedParams() =>
        Linq2dbBenchmark.Run(_linq2db);

    [Benchmark]
    [BenchmarkCategory(Builders)]
    public (string Sql, int ParameterCount) Sqlify_SpecificParams() =>
        SqlifyBenchmark.Run();

    [Benchmark]
    [BenchmarkCategory(Builders)]
    public (string Sql, int ParameterCount) SqlKata_SpecificParams() =>
        SqlKataBenchmark.Run();

    [Benchmark]
    [BenchmarkCategory(Builders)]
    public (string Sql, int ParameterCount) SqlArtisan_SpecificParams() =>
        SqlArtisanBenchmark.Run();

    [Benchmark]
    [BenchmarkCategory(Builders)]
    public (string Sql, int ParameterCount) SqlArtisan_DapperDynamicParams() =>
        SqlArtisanDapperBenchmark.Run();

    [Benchmark]
    [BenchmarkCategory(OrmReference)]
    public (string Sql, int ParameterCount) EfCore_Reference() =>
        EfCoreBenchmark.Run(_efCore);
}
