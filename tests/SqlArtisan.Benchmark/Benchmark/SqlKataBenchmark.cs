using SqlKata;

namespace SqlArtisan.Benchmark;

public static class SqlKataBenchmark
{
    public static void Run()
    {
        var query = new Query()
            .Select("a.Id", "COUNT(*) AS Count")
            .From("Authors a")
            .Join("Books b", j => j.On("a.Id", "b.AuthorId"))
            .Where("b.Rating", ">", 2.5)
            .Where("b.Rating", "<=", 5)
            .GroupBy("a.Id")
            .OrderByDesc("a.Id");

        var compiler = new SqlKata.Compilers.PostgresCompiler();
        var sql = compiler.Compile(query);
        // Parameters is List<object>
        var parameters = sql.Bindings;
    }
}
