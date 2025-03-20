using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class InnerJoinTest
{
	private readonly test_table _t = new("t");
	private readonly test_table _s = new("s");

	[Fact]
	public void INNER_JOIN_SimpleCondition_CorrectSql()
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.INNER_JOIN(_s)
			.ON(_t.code == _s.code)
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.name ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("INNER JOIN ");
		expected.Append("test_table s ");
		expected.Append("ON ");
		expected.Append("t.code = s.code");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void INNER_JOIN_ComplexConditionWithWhere_CorrectSql()
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.INNER_JOIN(_s)
			.ON(
				AND(
					_t.code == _s.code,
					_t.name == _s.name))
			.WHERE(_t.code > L(1))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.name ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("INNER JOIN ");
		expected.Append("test_table s ");
		expected.Append("ON ");
		expected.Append("(");
		expected.Append("t.code = s.code");
		expected.Append(") ");
		expected.Append("AND ");
		expected.Append("(");
		expected.Append("t.name = s.name");
		expected.Append(") ");
		expected.Append("WHERE ");
		expected.Append("t.code > 1");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
