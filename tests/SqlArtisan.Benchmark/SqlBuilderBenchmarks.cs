using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using LinqToDB.Data;
using SqlArtisan.Benchmark.EfCoreModel;

namespace SqlArtisan.Benchmark;

// Returning the (Sql, ParameterCount) tuple is what stops BenchmarkDotNet
// dead-code-eliminating the work. The Baseline and ORM-reference entrants are labeled out
// of the comparison rather than dropped: a floor and a scale marker. See README.md.
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
