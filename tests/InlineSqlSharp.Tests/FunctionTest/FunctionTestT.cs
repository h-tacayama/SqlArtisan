using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void SELECT_TO_DATE()
	{
		SqlCommand sql =
			SELECT(TO_DATE(L("2001/02/03"), L("YYYY/MM/DD")))
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

	[Fact]
	public void SELECT_TO_CHAR_DateTime_NoFormat()
	{
		SqlCommand sql =
			SELECT(TO_CHAR(_t.created_at))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("TO_CHAR(t.created_at)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_TO_CHAR_DateTime()
	{
		SqlCommand sql =
			SELECT(TO_CHAR(_t.created_at, L("YYYY-MM-DD")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("TO_CHAR(t.created_at, 'YYYY-MM-DD')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_TO_CHAR_Numeric_NoFormat()
	{
		SqlCommand sql =
			SELECT(TO_CHAR(_t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("TO_CHAR(t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_TO_CHAR_Numeric()
	{
		SqlCommand sql =
			SELECT(TO_CHAR(_t.code, L("999")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("TO_CHAR(t.code, '999')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
