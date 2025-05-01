using BenchmarkDotNet.Attributes;

namespace SqlArtisan.Benchmark;

[MemoryDiagnoser]
[ShortRunJob]
[MinColumn, MaxColumn]
public class Benchmarker
{
    [Benchmark(Baseline = true)]
    public void StringBuilder_BaseLine() => StringBuilderExample.Do();

    [Benchmark]
    public void DapperQbNet_NoParameters() => DapperQbNetExample.Do();

    [Benchmark]
    public void DapperSqlBuilder_DynamicParameters() => DapperSqlBuilderExample.Do();

    [Benchmark]
    public void SqExpress_NoParameters() => SqExpressExample.Do();

    [Benchmark]
    public void Sqlify_DictionaryOfStringObject() => SqlifyExample.Do();

    [Benchmark]
    public void SqlKata_ListOfObject() => SqlKataExample.Do();

    [Benchmark]
    public void SqlArtisan_DictionaryOfStringBindValue() => SqlArtisanExample.Do();

    [Benchmark]
    public void SqlArtisan_DynamicParameters() => SqlArtisanDapperExtensionsExample.Do();
}
