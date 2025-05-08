using SqExpress;
using SqExpress.SqlExport;
using SqlArtisan.Benchmark.SqExpressTable;
using static SqExpress.SqQueryBuilder;

namespace SqlArtisan.Benchmark;

public static class SqExpressBenchmark
{
    public static void Run()
    {
        var a = new Authors("a");
        var b = new Books("b");

        var query = Select(
            a.Id,
            Count(a.Id).As("Count"))
            .From(a)
            .InnerJoin(b, a.Id == b.AuthorId)
            .Where(b.Rating > 2.5 & b.Rating <= 5)
            .GroupBy(a.Id)
            .OrderBy(Desc(a.Id));

        var sql = query.ToSql(PgSqlExporter.Default);
        // No parameters
    }
}
