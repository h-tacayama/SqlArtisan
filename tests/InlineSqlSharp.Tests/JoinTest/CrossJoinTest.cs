using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class CrossJoinTest
{
	private readonly test_table _t = new("t");
	private readonly test_table _s = new("s");

	[Fact]
	public void CROSS_JOIN_ON()
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.CROSS_JOIN(_s)
			.WHERE(_t.code == _s.code)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("CROSS JOIN");
		expected.AppendLine("test_table s");
		expected.AppendLine("WHERE");
		expected.Append("t.code = s.code");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
