using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class LeftJoinTest
{
	private readonly test_table _t = new("t");
	private readonly test_table _s = new("s");

	[Fact]
	public void LEFT_JOIN_SimpleCondition_CorrectSql()
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.LEFT_JOIN(_s)
			.ON(_t.code == _s.code)
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.name ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("LEFT JOIN ");
		expected.Append("test_table s ");
		expected.Append("ON ");
		expected.Append("t.code = s.code");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
