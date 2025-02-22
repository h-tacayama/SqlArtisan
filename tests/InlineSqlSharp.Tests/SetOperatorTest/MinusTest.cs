using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class MinusTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void SELECT_MINUS()
	{
		SqlCommand sql =
			SELECT(L(1))
			.MINUS
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("1");
		expected.AppendLine("MINUS");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_MINUS()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.MINUS
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("MINUS");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_WHERE_MINUS()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.MINUS
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("WHERE");
		expected.AppendLine("t.code = 1");
		expected.AppendLine("MINUS");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_WHERE_GROUP_BY_MINUS()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.GROUP_BY(_t.code)
			.MINUS
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("WHERE");
		expected.AppendLine("t.code = 1");
		expected.AppendLine("GROUP BY");
		expected.AppendLine("t.code");
		expected.AppendLine("MINUS");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_WHERE_GROUP_BY_HAVING_MINUS()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.GROUP_BY(_t.code)
			.HAVING(COUNT(_t.code) > L(0))
			.MINUS
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("WHERE");
		expected.AppendLine("t.code = 1");
		expected.AppendLine("GROUP BY");
		expected.AppendLine("t.code");
		expected.AppendLine("HAVING");
		expected.AppendLine("COUNT(t.code) > 0");
		expected.AppendLine("MINUS");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_MINUS_ALL()
	{
		SqlCommand sql =
			SELECT(L(1))
			.MINUS_ALL
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("1");
		expected.AppendLine("MINUS ALL");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
