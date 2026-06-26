using System.Reflection;
using SqlArtisan.Benchmark.SqlifyTable;
using Sqlify.Core;
using static Sqlify.Sql;

namespace SqlArtisan.Benchmark;

public static class SqlifyBenchmark
{
    // Sqlify accumulates bind values in a private SqlWriter._params dictionary and
    // exposes no public accessor (only GetParam(name)). Read it via a cached
    // FieldInfo so the entrant reports a real parameter count for fair comparison;
    // the field already exists, so this is a single read with no extra allocation.
    private static readonly FieldInfo s_paramsField =
        typeof(SqlWriter).GetField("_params", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static (string Sql, int ParameterCount) Run()
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

        string sql = writer.GetCommand();
        IDictionary<string, object> parameters = (IDictionary<string, object>)s_paramsField.GetValue(writer)!;

        return (sql, parameters.Count);
    }
}
