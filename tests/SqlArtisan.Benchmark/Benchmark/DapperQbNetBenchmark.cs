using Viten.QueryBuilder;
using Viten.QueryBuilder.Renderer;

namespace SqlArtisan.Benchmark;

public static class DapperQbNetBenchmark
{
    public static void Run()
    {
        var a = From.Table("Authors", "a");
        var b = From.Table("Books", "b");

        var query = Qb.Select("a.Id, COUNT(*) AS Count")
            .From(a)
            .JoinInner(a, b, JoinCond.Fields("Id", "AuthorId"))
            .Where(Cond.Greater("b.Rating", 2.5), Cond.LessOrEqual("b.Rating", 5))
            .GroupBy("a.Id")
            .OrderBy("a.Id", OrderByDir.Desc);

        var pg = new PostgreSqlRenderer();
        var sql = pg.RenderSelect(query);
        // No parameters
    }
}
