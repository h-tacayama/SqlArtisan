using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class MinusTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void MINUS_SimpleSelect_CorrectSql()
	{
		SqlCommand sql =
			SELECT(L(1))
			.MINUS
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("1 ");
		expected.Append("MINUS ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void MINUS_SelectWithFrom_CorrectSql()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.MINUS
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.code ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("MINUS ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void MINUS_SelectWithFromWhere_CorrectSql()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.MINUS
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.code ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("WHERE ");
		expected.Append("t.code = 1 ");
		expected.Append("MINUS ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void MINUS_SelectWithFromWhereGroupBy_CorrectSql()
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
		expected.Append("SELECT ");
		expected.Append("t.code ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("WHERE ");
		expected.Append("t.code = 1 ");
		expected.Append("GROUP BY ");
		expected.Append("t.code ");
		expected.Append("MINUS ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void MINUS_SelectWithFromWhereGroupByHaving_CorrectSql()
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
		expected.Append("MINUS ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void MINUS_ALL_SimpleSelect_CorrectSql()
	{
		SqlCommand sql =
			SELECT(L(1))
			.MINUS_ALL
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("1 ");
		expected.Append("MINUS ALL ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
