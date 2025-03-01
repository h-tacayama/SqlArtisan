using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void SELECT_SUM()
	{
		SqlCommand sql =
			SELECT(SUM(_t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("SUM(t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SUM_DISTINCT()
	{
		SqlCommand sql =
			SELECT(SUM(DISTINCT, _t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("SUM(DISTINCT t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
