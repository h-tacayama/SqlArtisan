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
	public void SELECT_LEAST_Character()
	{
		SqlCommand sql =
			SELECT(LEAST(_t.name, L("test"), _t.name))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("LEAST(t.name, 'test', t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_LEAST_DateTime()
	{
		SqlCommand sql =
			SELECT(LEAST(
				_t.created_at,
				TO_DATE(L("2000/01/01"), L("YYYY/MM/DD")),
				_t.created_at))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("LEAST(t.created_at, TO_DATE('2000/01/01', 'YYYY/MM/DD'), t.created_at)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_LEAST_Numeric()
	{
		SqlCommand sql =
			SELECT(LEAST(_t.code, L(10), _t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("LEAST(t.code, 10, t.code)");

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
