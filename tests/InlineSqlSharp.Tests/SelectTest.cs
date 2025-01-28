using System.Text;
using InlineSqlSharp.Oracle;
using static InlineSqlSharp.Oracle.SqlWordbook;
namespace InlineSqlSharp.Tests;

public class SelectTest
{
	[Fact]
	public void SELECT_FROM_Basic()
	{
		test_table t = new("a");
		SqlCommand sql =
			SELECT(
				t.code,
				t.name,
				t.created_at)
			.FROM(t)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("a.code");
		expected.AppendLine(", a.name");
		expected.AppendLine(", a.created_at");
		expected.AppendLine("FROM");
		expected.Append("test_table a");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
