using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void SELECT_LAST_DAY()
	{
		SqlCommand sql =
			SELECT(LAST_DAY(_t.created_at))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("LAST_DAY(t.created_at)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
	
	[Fact]
	public void SELECT_LENGTH()
	{
		SqlCommand sql =
			SELECT(LENGTH(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("LENGTH(t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_LOWER()
	{
		SqlCommand sql =
			SELECT(LOWER(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("LOWER(t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_LPAD()
	{
		SqlCommand sql =
			SELECT(LPAD(_t.name, L(10)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("LPAD(t.name, 10)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_LPAD_Padding()
	{
		SqlCommand sql =
			SELECT(LPAD(_t.name, L(10), L("a")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("LPAD(t.name, 10, 'a')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_LTRIM()
	{
		SqlCommand sql =
			SELECT(LTRIM(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("LTRIM(t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_LTRIM_TrimChars()
	{
		SqlCommand sql =
			SELECT(LTRIM(_t.name, L("a")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("LTRIM(t.name, 'a')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
