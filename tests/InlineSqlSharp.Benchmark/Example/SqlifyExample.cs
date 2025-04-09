using InlineSqlSharp.Benchmark.SqlifyTable;
using Sqlify.Core;
using static Sqlify.Sql;

namespace InlineSqlSharp.Benchmark;

public static class SqlifyExample
{
    public static void Do()
    {
        var a = Table<IAuthors>("a");
        var b = Table<IBooks>("b");

        var selectQuery =
            Select(a.Id, Count().As("Count"))
            .From(a)
            .Join(b, a.Id == b.AuthorId)
            .Where(b.Rating > 2.5)
            .GroupBy(a.Id)
            .OrderByDesc(a.Id);

        var writer = new SqlWriter();
        selectQuery.Format(writer);
        var sql = writer.GetCommand();
    }
}
