using SqExpress;
using SqExpress.SqlExport;
using SqlArtisan.Benchmark.SqExpressTable;
using static SqExpress.SqQueryBuilder;

namespace SqlArtisan.Benchmark;

public static class SqExpressBenchmark
{
    public static void Run()
    {
        var u = new Users("u");
        var o = new Orders("o");

        var order_count = CustomColumnFactory.Int32("order_count");

        var query =
            Select(
                u.Id.As("user_id"),
                u.Name.As("user_name"),
                Count(o.Id).As(order_count))
            .From(u)
            .InnerJoin(o, u.Id == o.UserId)
            .Where(
                o.OrderDate >= new DateTime(2024, 1, 1)
                & o.OrderDate < new DateTime(2025, 1, 1))
            .GroupBy(u.Id, u.Name)
            .OrderBy(order_count);

        var sql = query.ToSql(PgSqlExporter.Default);
        // No parameters
    }
}
