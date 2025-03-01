using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void SELECT_CONCAT()
	{
		SqlCommand sql =
			SELECT(CONCAT(_t.name, L("a"), L("b")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("CONCAT(t.name, 'a', 'b')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

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
