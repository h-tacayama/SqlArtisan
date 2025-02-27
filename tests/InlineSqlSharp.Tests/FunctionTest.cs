using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class FunctionTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void SELECT_AVG()
	{
		SqlCommand sql =
			SELECT(AVG(_t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("AVG(t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_AVG_DISTINCT()
	{
		SqlCommand sql =
			SELECT(AVG(DISTINCT, _t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("AVG(DISTINCT t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

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

	[Fact]
	public void SELECT_TO_DATE()
	{
		SqlCommand sql =
			SELECT(TO_DATE("2001/02/03", "YYYY/MM/DD"))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("TO_DATE('2001/02/03', 'YYYY/MM/DD')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_MAX_Character()
	{
		SqlCommand sql =
			SELECT(MAX(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("MAX(t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_MAX_DateTime()
	{
		SqlCommand sql =
			SELECT(MAX(_t.created_at))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("MAX(t.created_at)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_MAX_Numeric()
	{
		SqlCommand sql =
			SELECT(MAX(_t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("MAX(t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_MIN_Character()
	{
		SqlCommand sql =
			SELECT(MIN(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("MIN(t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_MIN_DateTime()
	{
		SqlCommand sql =
			SELECT(MIN(_t.created_at))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("MIN(t.created_at)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_MIN_Numeric()
	{
		SqlCommand sql =
			SELECT(MIN(_t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("MIN(t.code)");

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
