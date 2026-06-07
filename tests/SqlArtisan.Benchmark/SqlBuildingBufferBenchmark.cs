using BenchmarkDotNet.Attributes;
using SqlArtisan.Benchmark.SqlArtisanTable;
using static SqlArtisan.Sql;

namespace SqlArtisan.Benchmark;

// Isolates the SqlBuildingBuffer/format path: the SqlPart tree is built once in
// [GlobalSetup], and only .Build() (dialect creation + buffer formatting + string
// + parameter dictionary) is measured in the loop.
[MemoryDiagnoser]
public class SqlBuildingBufferBenchmark
{
    private ISqlBuilder _builder = null!;

    [GlobalSetup]
    public void Setup()
    {
        Users u = new("u");
        Orders o = new("o");

        _builder =
            Select(
                u.Id.As("user_id"),
                u.Name.As("user_name"),
                Count(o.Id).As("order_count"))
            .From(u)
            .InnerJoin(o)
            .On(u.Id == o.UserId)
            .Where(
                o.OrderDate >= new DateTime(2024, 1, 1)
                & o.OrderDate < new DateTime(2025, 1, 1))
            .GroupBy(u.Id, u.Name)
            .OrderBy(Count(o.Id).As("order_count").Desc);
    }

    [Benchmark]
    public SqlStatement Build() => _builder.Build();
}
