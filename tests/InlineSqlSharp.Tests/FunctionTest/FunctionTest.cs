using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void SELECT_REGEXP_COUNT()
	{
		SqlCommand sql =
			SELECT(REGEXP_COUNT(_t.name, L("[abc]")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_COUNT(t.name, '[abc]')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REGEXP_COUNT_Position()
	{
		SqlCommand sql =
			SELECT(REGEXP_COUNT(_t.name, L("[abc]"), L(2)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_COUNT(t.name, '[abc]', 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REGEXP_COUNT_Position_Options()
	{
		SqlCommand sql =
			SELECT(REGEXP_COUNT(_t.name, L("[abc]"), L(2), RegexpOptions.CaseInsensitive))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_COUNT(t.name, '[abc]', 2, 'i')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_RPAD()
	{
		SqlCommand sql =
			SELECT(RPAD(_t.name, L(10)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("RPAD(t.name, 10)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_RPAD_Padding()
	{
		SqlCommand sql =
			SELECT(RPAD(_t.name, L(10), L("a")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("RPAD(t.name, 10, 'a')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_RTRIM()
	{
		SqlCommand sql =
			SELECT(RTRIM(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("RTRIM(t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_RTRIM_TrimChars()
	{
		SqlCommand sql =
			SELECT(RTRIM(_t.name, L("a")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("RTRIM(t.name, 'a')");

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
}
