using System.Text;
using Dapper;

namespace InlineSqlSharp.Benchmark;

public static class DapperSqlBuilderExample
{
	public static void Do()
	{
		var template = new StringBuilder();
		template.AppendLine("SELECT a.Id, COUNT(*) AS Count");
		template.Append("FROM Authors a");
		template.Append("/**innerjoin**/");
		template.Append("/**where**/");
		template.Append("/**groupby**/");
		template.Append("/**orderby**/");

		var builder = new SqlBuilder()
			.InnerJoin("Books b ON b.AuthorId = a.Id")
			.Where("b.Rating > @p1", new { p1 = 2.5 })
			.GroupBy("a.Id")
			.OrderBy("a.Id DESC");

		var query = builder.AddTemplate(template.ToString());
		var sql = query.RawSql;
		var parameters = query.Parameters;
	}
}
