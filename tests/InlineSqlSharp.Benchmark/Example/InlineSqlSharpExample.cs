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
            SELECT(a.Id, COUNT(a.Id).AS("Count"))
            .FROM(a)
            .INNER_JOIN(b)
            .ON(a.Id == b.AuthorId)
            .WHERE(b.Rating > 2.5)
            .GROUP_BY(a.Id)
            .ORDER_BY(a.Id.DESC)
            .Build();
    }
}
