using SqlKata;

namespace InlineSqlSharp.Benchmark;

public static class SqlKataExample
{
    public static void Do()
    {
        var query = new Query()
            .Select("a.Id", "COUNT(*) AS Count")
            .From("Authors a")
            .Join("Books b", j => j.On("a.Id", "b.AuthorId"))
            .Where("b.Rating", ">", 2.5)
            .GroupBy("a.Id")
            .OrderByDesc("a.Id");

        var compiler = new SqlKata.Compilers.PostgresCompiler();
        var sql = compiler.Compile(query);
    }
}
