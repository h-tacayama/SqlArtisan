using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class UnionTest
{
	private test_table _t = new("t");

	[Fact]
	public void SELECT_UNION()
	{
		SqlCommand sql =
			SELECT(L(1))
			.UNION
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("1");
		expected.AppendLine("UNION");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_UNION()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.UNION
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("UNION");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_WHERE_UNION()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.UNION
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("WHERE");
		expected.AppendLine("t.code = 1");
		expected.AppendLine("UNION");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_WHERE_GROUP_BY_UNION()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.GROUP_BY(_t.code)
			.UNION
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
		expected.AppendLine("UNION");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_WHERE_GROUP_BY_HAVING_UNION()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.GROUP_BY(_t.code)
			.HAVING(COUNT(_t.code) > L(0))
			.UNION
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
		expected.AppendLine("UNION");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_UNION_ALL()
	{
		SqlCommand sql =
			SELECT(L(1))
			.UNION_ALL
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("1");
		expected.AppendLine("UNION ALL");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
