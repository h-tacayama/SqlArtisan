namespace InlineSqlSharp.Benchmark;

using InlineSqlSharp.Benchmark.InlineSqlSharpTable;
using static InlineSqlSharp.SqlWordbook;

public static class InlineSqlSharpExample
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
            .Where(b.Rating > 2.5)
            .GroupBy(a.Id)
            .OrderBy(a.Id.Desc)
            .Build();
    }
}
