using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class IntersectTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void INTERSECT_SimpleSelect_CorrectSql()
	{
		SqlStatement sql =
			SELECT(L(1))
			.INTERSECT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("1 ");
		expected.Append("INTERSECT ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void INTERSECT_SelectWithFrom_CorrectSql()
	{
		SqlStatement sql =
			SELECT(_t.code)
			.FROM(_t)
			.INTERSECT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.code ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("INTERSECT ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void INTERSECT_SelectWithFromWhere_CorrectSql()
	{
		SqlStatement sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.INTERSECT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.code ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("WHERE ");
		expected.Append("t.code = 1 ");
		expected.Append("INTERSECT ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void INTERSECT_SelectWithFromWhereGroupBy_CorrectSql()
	{
		SqlStatement sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.GROUP_BY(_t.code)
			.INTERSECT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.code ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("WHERE ");
		expected.Append("t.code = 1 ");
		expected.Append("GROUP BY ");
		expected.Append("t.code ");
		expected.Append("INTERSECT ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void INTERSECT_SelectWithFromWhereGroupByHaving_CorrectSql()
	{
		SqlStatement sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.GROUP_BY(_t.code)
			.HAVING(COUNT(_t.code) > L(0))
			.INTERSECT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.code ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("WHERE ");
		expected.Append("t.code = 1 ");
		expected.Append("GROUP BY ");
		expected.Append("t.code ");
		expected.Append("HAVING ");
		expected.Append("COUNT(t.code) > 0 ");
		expected.Append("INTERSECT ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void INTERSECT_ALL_SimpleSelect_CorrectSql()
	{
		SqlStatement sql =
			SELECT(L(1))
			.INTERSECT_ALL
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("1 ");
		expected.Append("INTERSECT ALL ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Text);
	}
}
