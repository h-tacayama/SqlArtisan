using System.Text;
using Dapper;

namespace SqlArtisan.Benchmark;

public static class StringBuilderBenchmark
{
    public static void Run()
    {
        StringBuilder query = new();
        query.Append("SELECT ");
        query.Append("u.id AS user_id, ");
        query.Append("u.name AS user_name, ");
        query.Append("COUNT(o.id) AS order_count ");
        query.Append("FROM ");
        query.Append("users u ");
        query.Append("JOIN ");
        query.Append("orders o ");
        query.Append("ON ");
        query.Append("u.id = o.user_id ");
        query.Append("WHERE ");
        query.Append("o.order_date >= @0 ");
        query.Append("AND o.order_date < @1 ");
        query.Append("GROUP BY ");
        query.Append("u.id, u.name ");
        query.Append("ORDER BY ");
        query.Append("order_count DESC");

        DynamicParameters parameters = new();
        parameters.Add("@0", new DateTime(2024, 1, 1));
        parameters.Add("@1", new DateTime(2025, 1, 1));
    }
}
