using System.Text;
using Dapper;

namespace SqlArtisan.Benchmark;

public static class DapperSqlBuilderBenchmark
{
    public static (string Sql, int ParameterCount) Run()
    {
        var template = new StringBuilder();
        template.AppendLine("SELECT u.id AS user_id, u.name AS user_name, COUNT(o.id) AS order_count");
        template.Append("FROM users u");
        template.Append("/**innerjoin**/");
        template.Append("/**where**/");
        template.Append("/**groupby**/");
        template.Append("/**orderby**/");

        DynamicParameters parameters = new();
        parameters.Add("p0", new DateTime(2024, 1, 1));
        parameters.Add("p1", new DateTime(2025, 1, 1));

        SqlBuilder builder = new SqlBuilder()
            .InnerJoin("orders o ON u.id = o.user_id")
            .Where("o.order_date >= @p0")
            .Where("o.order_date < @p1")
            .GroupBy("u.id, u.name")
            .OrderBy("order_count DESC");

        SqlBuilder.Template query = builder.AddTemplate(template.ToString(), parameters);

        string sql = query.RawSql;

        return (sql, parameters.ParameterNames.Count());
    }
}
