using SqlKata;

namespace SqlArtisan.Benchmark;

public static class SqlKataBenchmark
{
    // Hoisted like the other builders' fixed setup: the compiler is reusable
    // state, and constructing it per iteration would overstate SqlKata's cost.
    private static readonly SqlKata.Compilers.PostgresCompiler Compiler = new();

    public static (string Sql, int ParameterCount) Run()
    {
        // Every clause builder quotes what it is given as one identifier, so the aggregate
        // needs SelectRaw and each GROUP BY key its own argument (#382).
        Query query = new Query()
            .Select("users.id AS user_id", "users.name AS user_name")
            .SelectRaw("COUNT(orders.id) AS order_count")
            .From("users")
            .Join("orders", j => j.On("users.id", "orders.user_id"))
            .Where("orders.order_date", ">=", new DateTime(2024, 1, 1))
            .Where("orders.order_date", "<", new DateTime(2025, 1, 1))
            .GroupBy("users.id", "users.name")
            .OrderByDesc("order_count");

        SqlResult sql = Compiler.Compile(query);
        List<object> parameters = sql.Bindings;

        return (sql.Sql, parameters.Count);
    }
}
