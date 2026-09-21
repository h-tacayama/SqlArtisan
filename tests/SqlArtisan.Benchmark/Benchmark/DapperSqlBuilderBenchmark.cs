using Dapper;

namespace SqlArtisan.Benchmark;

public static class DapperSqlBuilderBenchmark
{
    // A constant, as a caller would write it: assembling the template per call
    // would charge this entrant for scaffolding no other entrant pays.
    private const string Template =
        "SELECT u.id AS user_id, u.name AS user_name, COUNT(o.id) AS order_count\n"
        + "FROM users u/**innerjoin**//**where**//**groupby**//**orderby**/";

    public static (string Sql, int ParameterCount) Run()
    {
        DynamicParameters parameters = new();
        parameters.Add("p0", new DateTime(2024, 1, 1));
        parameters.Add("p1", new DateTime(2025, 1, 1));

        SqlBuilder builder = new SqlBuilder()
            .InnerJoin("orders o ON u.id = o.user_id")
            .Where("o.order_date >= @p0")
            .Where("o.order_date < @p1")
            .GroupBy("u.id, u.name")
            .OrderBy("order_count DESC");

        SqlBuilder.Template query = builder.AddTemplate(Template, parameters);

        string sql = query.RawSql;

        return (sql, parameters.ParameterNames.Count());
    }
}
