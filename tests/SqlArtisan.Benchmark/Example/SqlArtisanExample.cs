namespace InlineSqlSharp.Benchmark;

using InlineSqlSharp.Benchmark.InlineSqlSharpTable;
using static InlineSqlSharp.SqlWordbook;

public static class SqlArtisanExample
{
    public static void Do()
    {
        Authors a = new("a");
        Books b = new("b");

        SqlStatement sql =
            Select(a.Id, Count(a.Id).As("Count"))
            .From(a)
            .InnerJoin(b)
            .On(a.Id == b.AuthorId)
            .Where(And(b.Rating > 2.5, b.Rating <= 5))
            .GroupBy(a.Id)
            .OrderBy(a.Id.Desc)
            .Build();
    }
}
