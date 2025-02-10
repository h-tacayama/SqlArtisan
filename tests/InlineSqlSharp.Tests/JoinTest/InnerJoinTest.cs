using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class InnerJoinTest
{
	private test_table _t = new("t");
	private test_table _s = new("s");

	[Fact]
	public void INNER_JOIN_ON()
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.INNER_JOIN(_s)
			.ON(_t.code == _s.code)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("INNER JOIN");
		expected.AppendLine("test_table s");
		expected.AppendLine("ON");
		expected.Append("t.code = s.code");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void INNER_JOIN_ON_WHERE()
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
		expected.AppendLine("SELECT");
		expected.AppendLine("t.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("INNER JOIN");
		expected.AppendLine("test_table s");
		expected.AppendLine("ON");
		expected.AppendLine("(");
		expected.AppendLine("t.code = s.code");
		expected.AppendLine(")");
		expected.AppendLine("AND");
		expected.AppendLine("(");
		expected.AppendLine("t.name = s.name");
		expected.AppendLine(")");
		expected.AppendLine("WHERE");
		expected.Append("t.code > 1");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
