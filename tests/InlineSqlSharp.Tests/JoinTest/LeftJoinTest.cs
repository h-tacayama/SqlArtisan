using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class LeftJoinTest
{
	private test_table _t = new("t");
	private test_table _s = new("s");

	[Fact]
	public void LEFT_JOIN_ON()
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.LEFT_JOIN(_s)
			.ON(_t.code == _s.code)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("LEFT JOIN");
		expected.AppendLine("test_table s");
		expected.AppendLine("ON");
		expected.Append("t.code = s.code");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
