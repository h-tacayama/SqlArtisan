using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class ExceptTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void EXCEPT_SimpleSelect_CorrectSql()
	{
		SqlCommand sql =
			SELECT(L(1))
			.EXCEPT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("1 ");
		expected.Append("EXCEPT ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void EXCEPT_SelectWithFrom_CorrectSql()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.EXCEPT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.code ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("EXCEPT ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void EXCEPT_SelectWithFromWhere_CorrectSql()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.EXCEPT
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.code ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("WHERE ");
		expected.Append("t.code = 1 ");
		expected.Append("EXCEPT ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void EXCEPT_SelectWithFromWhereGroupBy_CorrectSql()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.GROUP_BY(_t.code)
			.EXCEPT
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
		expected.Append("EXCEPT ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void EXCEPT_SelectWithFromWhereGroupByHaving_CorrectSql()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == L(1))
			.GROUP_BY(_t.code)
			.HAVING(COUNT(_t.code) > L(0))
			.EXCEPT
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
		expected.Append("EXCEPT ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void EXCEPT_ALL_SimpleSelect_CorrectSql()
	{
		SqlCommand sql =
			SELECT(L(1))
			.EXCEPT_ALL
			.SELECT(L(2))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("1 ");
		expected.Append("EXCEPT ALL ");
		expected.Append("SELECT ");
		expected.Append("2");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
