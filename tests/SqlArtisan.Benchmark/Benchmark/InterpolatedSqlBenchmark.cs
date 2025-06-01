using InterpolatedSql;
using InterpolatedSql.SqlBuilders;

namespace SqlArtisan.Benchmark;

public static class InterpolatedSqlBenchmark
{
    public static void Run()
    {
        IInterpolatedSql query = new SqlBuilder($@"SELECT u.id AS user_id,
u.name AS user_name,
COUNT(o.id) AS order_count
FROM users u
INNER JOIN orders o
ON u.id = o.user_id
WHERE(o.order_date >= {new DateTime(2024, 1, 1)})
AND(o.order_date < {new DateTime(2025, 1, 1)})
GROUP BY u.id, u.name
ORDER BY order_count DESC").Build();

#pragma warning disable IDE0059
        string sql = query.Sql;
        IReadOnlyList<InterpolatedSqlParameter> parameters =
            query.SqlParameters;
#pragma warning restore IDE0059
    }
}
