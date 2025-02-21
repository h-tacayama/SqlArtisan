using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class FunctionTest
{
	private test_table _t = new("t");

	[Fact]
	public void SELECT_COUNT()
	{
		SqlCommand sql =
			SELECT(COUNT(_t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("COUNT(t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_COUNT_DISTINCT()
	{
		SqlCommand sql =
			SELECT(COUNT(DISTINCT, _t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("COUNT(DISTINCT t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
