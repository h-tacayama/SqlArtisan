using BenchmarkDotNet.Attributes;
using SqlArtisan.Benchmark.SqlArtisanTable;
using static SqlArtisan.Sql;

namespace SqlArtisan.Benchmark;

// What a registered value converter costs one bound value against writing .Value, on the
// same statement (#810). SqlArtisan alone: no other builder takes a value object.
[MemoryDiagnoser]
public class ValueConverterBenchmarks
{
    private static readonly UserId Id = new(42);

    [GlobalSetup]
    public void Setup() => SqlArtisanConfig.AddValueConverter<UserId>(id => id.Value);

    [GlobalCleanup]
    public void Cleanup() => SqlArtisanConfig.ResetValueConverters();

    [Benchmark(Baseline = true)]
    public (string Sql, int ParameterCount) WrappedValue()
    {
        Users u = new("u");
        SqlStatement sql = Select(u.Name).From(u).Where(u.Id == Id.Value).Build();
        return (sql.Text, sql.Parameters.Count);
    }

    [Benchmark]
    public (string Sql, int ParameterCount) RegisteredValueObject()
    {
        Users u = new("u");
        SqlStatement sql = Select(u.Name).From(u).Where(u.Id == Id).Build();
        return (sql.Text, sql.Parameters.Count);
    }

    private readonly record struct UserId(long Value);
}
