using System.Text;
using Dapper;

namespace SqlArtisan.Benchmark;

public static class DapperSqlBuilderBenchmark
{
    public static void Run()
    {
        var template = new StringBuilder();
        template.AppendLine("SELECT u.id AS user_id, u.name AS user_name, COUNT(o.id) AS order_count");
        template.Append("FROM users u");
        template.Append("/**innerjoin**/");
        template.Append("/**where**/");
        template.Append("/**groupby**/");
        template.Append("/**orderby**/");

        SqlBuilder builder = new SqlBuilder()
            .InnerJoin("orders o ON u.id = o.user_id")
            .Where("o.order_date >= @p0", new { p0 = new DateTime(2024, 1, 1) })
            .Where("o.order_date < @p1", new { p1 = new DateTime(2025, 1, 1) })
            .GroupBy("u.id, u.name")
            .OrderBy("order_count DESC");

        SqlBuilder.Template query = builder.AddTemplate(template.ToString());

#pragma warning disable IDE0059
        string sql = query.RawSql;
        DynamicParameters? parameters = query.Parameters as DynamicParameters;
#pragma warning restore IDE0059
    }
}
