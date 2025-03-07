using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void SELECT_SUBSTR()
	{
		SqlCommand sql =
			SELECT(SUBSTR(_t.name, L(1)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("SUBSTR(t.name, 1)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SUBSTR_Length()
	{
		SqlCommand sql =
			SELECT(SUBSTR(_t.name, L(1), L(3)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("SUBSTR(t.name, 1, 3)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SUBSTRB()
	{
		SqlCommand sql =
			SELECT(SUBSTRB(_t.name, L(1)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("SUBSTRB(t.name, 1)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SUBSTRB_Length()
	{
		SqlCommand sql =
			SELECT(SUBSTRB(_t.name, L(1), L(3)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("SUBSTRB(t.name, 1, 3)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

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

	[Fact]
	public void SELECT_SYSDATE()
	{
		SqlCommand sql =
			SELECT(SYSDATE)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("SYSDATE");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SYSTIMESTAMP()
	{
		SqlCommand sql =
			SELECT(SYSTIMESTAMP)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("SYSTIMESTAMP");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
