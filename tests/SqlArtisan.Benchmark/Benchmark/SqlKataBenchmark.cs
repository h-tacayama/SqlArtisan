using SqlKata;

namespace SqlArtisan.Benchmark;

public static class SqlKataBenchmark
{
    public static void Run()
    {
        Query query = new Query()
            .Select("users.id AS user_id", "users.name AS user_name", "COUNT(orders.id) AS order_count")
            .From("users")
            .Join("orders", j => j.On("users.id", "orders.user_id"))
            .Where("orders.order_date", ">=", new DateTime(2024, 1, 1))
            .Where("orders.order_date", "<", new DateTime(2025, 1, 1))
            .GroupBy("users.id, users.name")
            .OrderByDesc("order_count");

        var compiler = new SqlKata.Compilers.PostgresCompiler();

        SqlResult sql = compiler.Compile(query);
        List<object> parameters = sql.Bindings;
    }
}
