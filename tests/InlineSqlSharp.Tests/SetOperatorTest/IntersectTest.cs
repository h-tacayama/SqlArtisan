using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class IntersectTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void SELECT_INTERSECT()
	{
		SqlCommand sql =
			SELECT(L(1))
			.INTERSECT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("1");
		expected.AppendLine("INTERSECT");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_INTERSECT()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.INTERSECT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("INTERSECT");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_WHERE_INTERSECT()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.INTERSECT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("WHERE");
		expected.AppendLine("t.code = 1");
		expected.AppendLine("INTERSECT");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_WHERE_GROUP_BY_INTERSECT()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.GROUP_BY(_t.code)
			.INTERSECT
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
		expected.AppendLine("INTERSECT");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_FROM_WHERE_GROUP_BY_HAVING_INTERSECT()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.GROUP_BY(_t.code)
			.HAVING(COUNT(_t.code) > L(0))
			.INTERSECT
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
		expected.AppendLine("INTERSECT");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_INTERSECT_ALL()
	{
		SqlCommand sql =
			SELECT(L(1))
			.INTERSECT_ALL
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("1");
		expected.AppendLine("INTERSECT ALL");
		expected.AppendLine("SELECT");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
