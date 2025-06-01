using SqlArtisan.Benchmark.SqlifyTable;
using Sqlify.Core;
using static Sqlify.Sql;

namespace SqlArtisan.Benchmark;

public static class SqlifyBenchmark
{
    public static void Run()
    {
        IUsers u = Table<IUsers>("u");
        IOrders o = Table<IOrders>("o");

        Sqlify.SelectQuery selectQuery =
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

        SqlWriter writer = new();
        selectQuery.Format(writer);

#pragma warning disable IDE0059
        string sql = writer.GetCommand();
        // parameters is Dictionary<string, object>
#pragma warning restore IDE0059
    }
}
