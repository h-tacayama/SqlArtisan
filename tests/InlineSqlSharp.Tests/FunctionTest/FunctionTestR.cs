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
	public void SELECT_REGEXP_REPLACE()
	{
		SqlCommand sql =
			SELECT(REGEXP_REPLACE(_t.name, L("[abc]"), L("x")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_REPLACE(t.name, '[abc]', 'x')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REGEXP_REPLACE_Position()
	{
		SqlCommand sql =
			SELECT(REGEXP_REPLACE(_t.name, L("[abc]"), L("x"), L(2)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_REPLACE(t.name, '[abc]', 'x', 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REGEXP_REPLACE_Position_Occurrence()
	{
		SqlCommand sql =
			SELECT(REGEXP_REPLACE(_t.name, L("[abc]"), L("x"), L(2), L(3)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_REPLACE(t.name, '[abc]', 'x', 2, 3)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REGEXP_REPLACE_Position_Occurrence_Options()
	{
		SqlCommand sql =
			SELECT(REGEXP_REPLACE(_t.name, L("[abc]"), L("x"), L(2), L(3), RegexpOptions.CaseInsensitive))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_REPLACE(t.name, '[abc]', 'x', 2, 3, 'i')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REGEXP_SUBSTR()
	{
		SqlCommand sql =
			SELECT(REGEXP_SUBSTR(_t.name, L("[abc]")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_SUBSTR(t.name, '[abc]')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REGEXP_SUBSTR_Position()
	{
		SqlCommand sql =
			SELECT(REGEXP_SUBSTR(_t.name, L("[abc]"), L(2)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_SUBSTR(t.name, '[abc]', 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REGEXP_SUBSTR_Position_Occurrence()
	{
		SqlCommand sql =
			SELECT(REGEXP_SUBSTR(_t.name, L("[abc]"), L(2), L(3)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_SUBSTR(t.name, '[abc]', 2, 3)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REGEXP_SUBSTR_Position_Occurrence_Options()
	{
		SqlCommand sql =
			SELECT(REGEXP_SUBSTR(_t.name, L("[abc]"), L(2), L(3), RegexpOptions.CaseInsensitive))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_SUBSTR(t.name, '[abc]', 2, 3, 'i')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REGEXP_SUBSTR_Position_Occurrence_Options_SubPattern()
	{
		SqlCommand sql =
			SELECT(REGEXP_SUBSTR(_t.name, L("[abc]"), L(2), L(3), RegexpOptions.None, L(1)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REGEXP_SUBSTR(t.name, '[abc]', 2, 3, '', 1)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_REPLACE()
	{
		SqlCommand sql =
			SELECT(REPLACE(_t.name, L("a"), L("b")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("REPLACE(t.name, 'a', 'b')");

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
}
