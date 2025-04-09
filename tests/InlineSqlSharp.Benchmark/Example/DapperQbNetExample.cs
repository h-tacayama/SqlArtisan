using Viten.QueryBuilder;
using Viten.QueryBuilder.Renderer;

namespace InlineSqlSharp.Benchmark;

public static class DapperQbNetExample
{
    public static void Do()
    {
        var a = From.Table("Authors", "a");
        var b = From.Table("Books", "b");

        var query = Qb.Select("a.Id, COUNT(*) AS Count")
            .From(a)
            .JoinInner(a, b, JoinCond.Fields("Id", "AuthorId"))
            .Where(Cond.Greater("b.Rating", 2.5))
            .GroupBy("a.Id")
            .OrderBy("a.Id", OrderByDir.Desc);

        var pg = new PostgreSqlRenderer();
        var sql = pg.RenderSelect(query);
    }
}
