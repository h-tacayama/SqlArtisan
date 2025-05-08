using SqlArtisan.Benchmark.SqlArtisanTable;
using static SqlArtisan.SqlWordbook;

namespace SqlArtisan.Benchmark;

public static class SqlArtisanBenchmark
{
    public static void Run()
    {
        Authors a = new("a");
        Books b = new("b");

        SqlStatement sql =
            Select(a.Id, Count(a.Id).As("Count"))
            .From(a)
            .InnerJoin(b)
            .On(a.Id == b.AuthorId)
            .Where(b.Rating > 2.5 & b.Rating <= 5)
            .GroupBy(a.Id)
            .OrderBy(a.Id.Desc)
            .Build();
        var sqlText = sql.Text;
        // Parameters is Dictionary<string, BindValue>
        var parameters = sql.Parameters;
    }
}
