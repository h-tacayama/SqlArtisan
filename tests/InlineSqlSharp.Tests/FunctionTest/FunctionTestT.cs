using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void SELECT_TO_DATE()
	{
		SqlCommand sql =
			SELECT(TO_DATE(L("2001/02/03"), "YYYY/MM/DD"))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("TO_DATE('2001/02/03', 'YYYY/MM/DD')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_TRIM()
	{
		SqlCommand sql =
			SELECT(TRIM(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("TRIM(t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_TRIM_TrimChar()
	{
		SqlCommand sql =
			SELECT(TRIM(_t.name, L("a")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("TRIM(BOTH 'a' FROM t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
