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
	public void SELECT_INSTR()
	{
		SqlCommand sql =
			SELECT(INSTR(_t.name, L("abc")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("INSTR(t.name, 'abc')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_INSTR_Position()
	{
		SqlCommand sql =
			SELECT(
				INSTR(_t.name, L("abc"), L(2)),
				INSTR(_t.name, L("abc"), L(1)),
				INSTR(_t.name, L("abc"), L(-1)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("INSTR(t.name, 'abc', 2)");
		expected.AppendLine(", INSTR(t.name, 'abc', 1)");
		expected.Append(", INSTR(t.name, 'abc', -1)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_INSTR_Occurrence()
	{
		SqlCommand sql =
			SELECT(
				INSTR(_t.name, L("abc"), L(1), L(1)),
				INSTR(_t.name, L("abc"), L(1), L(2)),
				INSTR(_t.name, L("abc"), L(2), L(-1)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("INSTR(t.name, 'abc', 1, 1)");
		expected.AppendLine(", INSTR(t.name, 'abc', 1, 2)");
		expected.Append(", INSTR(t.name, 'abc', 2, -1)");

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
	public void SELECT_UPPER()
	{
		SqlCommand sql =
			SELECT(UPPER(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("UPPER(t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
