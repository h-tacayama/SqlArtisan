using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class InsertTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void INSERT_INTO_SET()
	{
		SqlCommand sql =
			INSERT_INTO(_t)
			.SET(
				_t.code == L(1),
				_t.name == L("a"),
				_t.created_at == SYSDATE)
			.Build();

		StringBuilder expected = new();
		expected.Append("INSERT INTO ");
		expected.Append("test_table t ");
		expected.Append("(");
		expected.Append("t.code, ");
		expected.Append("t.name, ");
		expected.Append("t.created_at");
		expected.Append(") ");
		expected.Append("VALUES ");
		expected.Append("(");
		expected.Append("1, ");
		expected.Append("'a', ");
		expected.Append("SYSDATE");
		expected.Append(")");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void UPDATE_SET_Inequality_then_Throw_ArgumentException()
	{
		Assert.Throws<ArgumentException>(() =>
		{
			INSERT_INTO(_t)
			.SET(
				_t.code != L(1))
			.Build();
		});
	}

	[Fact]
	public void INSERT_INTO_SELECT()
	{
		test_table s = new("s");

		SqlCommand sql =
			INSERT_INTO(_t, _t.code, _t.name, _t.created_at)
			.SELECT(s.code, s.name, s.created_at)
			.FROM(s)
			.Build();

		StringBuilder expected = new();
		expected.Append("INSERT INTO ");
		expected.Append("test_table t ");
		expected.Append("(");
		expected.Append("t.code, ");
		expected.Append("t.name, ");
		expected.Append("t.created_at");
		expected.Append(") ");
		expected.Append("SELECT ");
		expected.Append("s.code, ");
		expected.Append("s.name, ");
		expected.Append("s.created_at ");
		expected.Append("FROM ");
		expected.Append("test_table s");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
