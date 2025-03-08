using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
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
	public void SELECT_MOD()
	{
		SqlCommand sql =
			SELECT(MOD(_t.code, L(3)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("MOD(t.code, 3)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_MONTHS_BETWEEN()
	{
		SqlCommand sql =
			SELECT(MONTHS_BETWEEN(
				TO_DATE(L("2001/02/03"), L("YYYY/MM/DD")),
				TO_DATE(L("2004/05/06"), L("YYYY/MM/DD"))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("MONTHS_BETWEEN(TO_DATE('2001/02/03', 'YYYY/MM/DD')");
		expected.Append(", TO_DATE('2004/05/06', 'YYYY/MM/DD'))");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
