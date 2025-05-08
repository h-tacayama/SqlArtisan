using BenchmarkDotNet.Attributes;

namespace SqlArtisan.Benchmark;

[MemoryDiagnoser]
[ShortRunJob]
[MinColumn, MaxColumn]
public class Benchmarker
{
    [Benchmark(Baseline = true)]
    public void StringBuilder_WithDynamicParameters() =>
        StringBuilderBenchmark.Run();

    [Benchmark]
    public void DapperQbNet_WithNoParameters() =>
        DapperQbNetBenchmark.Run();

    [Benchmark]
    public void DapperSqlBuilder_WithDynamicParameters() =>
        DapperSqlBuilderBenchmark.Run();

    [Benchmark]
    public void SqExpress_WithNoParameters() =>
        SqExpressBenchmark.Run();

    [Benchmark]
    public void Sqlify_WithParametersAsObject() =>
        SqlifyBenchmark.Run();

    [Benchmark]
    public void SqlKata_WithParametersAsObject() =>
        SqlKataBenchmark.Run();

    [Benchmark]
    public void SqlArtisan_WithParametersAsBindValue() =>
        SqlArtisanBenchmark.Run();

    [Benchmark]
    public void SqlArtisan_WithDynamicParameters() =>
        SqlArtisanDapperBenchmark.Run();
}
