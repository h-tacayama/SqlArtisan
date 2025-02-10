using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class FunctionTest
{
	private test_table _t = new("t");

	[Fact]
	public void Function_COUNT()
	{
		SqlCommand sql =
			SELECT(COUNT(_t.code))
			.FROM(_t)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("COUNT(t.code)");
		expected.AppendLine("FROM");
		expected.Append("test_table t");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
