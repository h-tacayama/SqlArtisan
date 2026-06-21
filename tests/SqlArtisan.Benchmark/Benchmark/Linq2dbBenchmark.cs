using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using SqlArtisan.Benchmark.Linq2dbTable;

namespace SqlArtisan.Benchmark;

public static class Linq2dbBenchmark
{
    // A dummy connection string is enough: pinning the PostgreSQL dialect skips the
    // server-version detection that would otherwise open a real connection, so SQL is
    // generated entirely offline.
    private const string ConnectionString =
        "Host=localhost;Database=benchmark;Username=benchmark;Password=benchmark";

    public static DataConnection CreateConnection()
    {
        DataOptions options =
            new DataOptions().UsePostgreSQL(ConnectionString, PostgreSQLVersion.v95);
        return new DataConnection(options);
    }

    public static (string Sql, int ParameterCount) Run(DataConnection db)
    {
        DateTime fromDate = new DateTime(2024, 1, 1);
        DateTime toDate = new DateTime(2025, 1, 1);

        var query =
            from u in db.GetTable<User>()
            join o in db.GetTable<Order>() on u.Id equals o.UserId
            where o.OrderDate >= fromDate && o.OrderDate < toDate
            group o by new { u.Id, u.Name } into g
            orderby g.Count() descending
            select new { g.Key.Id, g.Key.Name, OrderCount = g.Count() };

        QuerySql sql = query.ToSqlQuery();

        return (sql.Sql, sql.Parameters.Count);
    }
}
