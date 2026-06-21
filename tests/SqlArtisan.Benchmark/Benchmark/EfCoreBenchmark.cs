using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SqlArtisan.Benchmark.EfCoreModel;

namespace SqlArtisan.Benchmark;

// EF Core is a labeled ORM *reference*, not a builder entrant: it does materially more
// work (model metadata, change-tracking infrastructure, a richer query pipeline) and
// caches compiled queries, so it is reported in a separate category. SQL is generated
// via CreateDbCommand(), which produces the same text as ToQueryString() plus a real
// parameter collection, without opening a connection or executing.
public static class EfCoreBenchmark
{
    public static BenchmarkDbContext CreateContext() => new();

    public static (string Sql, int ParameterCount) Run(BenchmarkDbContext db)
    {
        DateTime fromDate = new(2024, 1, 1);
        DateTime toDate = new(2025, 1, 1);

        IQueryable<object> query = db.Orders
            .Where(o => o.OrderDate >= fromDate && o.OrderDate < toDate)
            .GroupBy(o => new { o.User.Id, o.User.Name })
            .Select(g => new { g.Key.Id, g.Key.Name, OrderCount = g.Count() })
            .OrderByDescending(x => x.OrderCount);

        using DbCommand command = query.CreateDbCommand();

        return (command.CommandText, command.Parameters.Count);
    }
}
