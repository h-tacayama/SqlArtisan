using Viten.QueryBuilder;
using Viten.QueryBuilder.Renderer;
using static Viten.QueryBuilder.Qb;

namespace SqlArtisan.Benchmark;

public static class DapperQbNetBenchmark
{
    public static void Run()
    {
        var u = From.Table("users", "u");
        var o = From.Table("orders ", "o");

        var query =
            Select(
                Column.New("id", u, "user_id"),
                Column.New("name", u, "user_name"),
                Column.New("id", "order_count", o, AggFunc.Count))
            .From(u)
            .JoinInner(u, o, JoinCond.Fields("id", "user_id"))
            .Where(
                Cond.GreaterOrEqual("o.order_date", new DateTime(2024, 1, 1)),
                Cond.Less("o.order_date", new DateTime(2025, 1, 1)))
            .GroupBy("u.id")
            .GroupBy("u.name")
            .OrderBy("order_count", OrderByDir.Desc);

        var pg = new PostgreSqlRenderer();
        var sql = pg.RenderSelect(query);
        // No parameters
    }
}
