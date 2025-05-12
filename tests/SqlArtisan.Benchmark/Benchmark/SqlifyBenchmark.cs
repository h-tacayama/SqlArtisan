using SqlArtisan.Benchmark.SqlifyTable;
using Sqlify.Core;
using static Sqlify.Sql;

namespace SqlArtisan.Benchmark;

public static class SqlifyBenchmark
{
    public static void Run()
    {
        var u = Table<IUsers>("u");
        var o = Table<IOrders>("o");

        var selectQuery =
            Select(
                u.Id.As("user_id"),
                u.Name.As("user_name"),
                Count().As("order_count"))
            .From(u)
            .Join(o, u.Id == o.UserId)
            .Where(o.OrderDate >= new DateTime(2024, 1, 1))
            .Where(o.OrderDate < new DateTime(2025, 1, 1))
            .GroupBy(u.Id)
            .GroupBy(u.Name)
            .OrderByDesc(Count());

        var writer = new SqlWriter();
        selectQuery.Format(writer);
        var sql = writer.GetCommand();
        // Parameters is Dictionary<string, object>
    }
}
