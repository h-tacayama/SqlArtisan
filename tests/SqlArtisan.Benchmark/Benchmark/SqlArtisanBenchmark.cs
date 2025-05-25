using SqlArtisan.Benchmark.SqlArtisanTable;
using static SqlArtisan.Sql;

namespace SqlArtisan.Benchmark;

public static class SqlArtisanBenchmark
{
    public static void Run()
    {
        Users u = new("u");
        Orders o = new("o");

        SqlStatement sql =
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
            .OrderBy(Count(o.Id).As("order_count").Desc)
            .Build();

        var sqlText = sql.Text;
        // Parameters is Dictionary<string, BindValue>
        var parameters = sql.Parameters;
    }
}
