using BenchmarkDotNet.Attributes;

namespace SqlArtisan.Benchmark;

[MemoryDiagnoser]
[ShortRunJob]
public class SqlBuilderBenchmarks
{
    [Benchmark]
    public void StringBuilder_DapperDynamicParams() =>
        StringBuilderBenchmark.Run();

    [Benchmark]
    public void DapperQbNet_NoParams() =>
        DapperQbNetBenchmark.Run();

    [Benchmark]
    public void DapperSqlBuilder_DapperDynamicParams() =>
        DapperSqlBuilderBenchmark.Run();

    [Benchmark]
    public void InterpolatedSql_SpecificParams() =>
        InterpolatedSqlBenchmark.Run();

    [Benchmark]
    public void SqExpress_NoParams() =>
        SqExpressBenchmark.Run();

    [Benchmark]
    public void Sqlify_SpecificParams() =>
        SqlifyBenchmark.Run();

    [Benchmark]
    public void SqlKata_SpecificParams() =>
        SqlKataBenchmark.Run();

    [Benchmark]
    public void SqlArtisan_SpecificParams() =>
        SqlArtisanBenchmark.Run();

    [Benchmark]
    public void SqlArtisan_DapperDynamicParams() =>
        SqlArtisanDapperBenchmark.Run();
}
