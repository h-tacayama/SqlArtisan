using InlineSqlSharp.Benchmark.SqExpressTable;
using SqExpress;
using SqExpress.SqlExport;
using static SqExpress.SqQueryBuilder;

namespace InlineSqlSharp.Benchmark;

public static class SqExpressExample
{
	public static void Do()
	{
		var a = new Authors("a");
		var b = new Books("b");

		var query = Select(
			a.Id,
			Count(a.Id).As("Count"))
			.From(a)
			.InnerJoin(b, a.Id == b.AuthorId)
			.Where(b.Rating > 2.5)
			.GroupBy(a.Id)
			.OrderBy(Desc(a.Id));

		var sql = query.ToSql(PgSqlExporter.Default);
	}
}
