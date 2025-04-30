using BenchmarkDotNet.Attributes;

namespace SqlArtisan.Benchmark;

[MemoryDiagnoser]
[ShortRunJob]
[MinColumn, MaxColumn]
public class Benchmarker
{
    [Benchmark]
    public void DapperQbNet() => DapperQbNetExample.Do();

    [Benchmark]
    public void DapperSqlBuilder() => DapperSqlBuilderExample.Do();

    [Benchmark]
    public void SqExpress() => SqExpressExample.Do();

    [Benchmark]
    public void Sqlify() => SqlifyExample.Do();

    [Benchmark]
    public void SqlKata() => SqlKataExample.Do();

    [Benchmark]
    public void SqlArtisan() => SqlArtisanExample.Do();

    [Benchmark]
    public void StringBuilderTest() => StringBuilderExample.Do();
}
