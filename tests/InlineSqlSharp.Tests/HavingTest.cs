using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class HavingTest
{
	private test_table _t = new("t");

	[Fact]
	public void HAVING_Single()
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.GROUP_BY(_t.name)
			.HAVING(COUNT(_t.name) > L(1))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("GROUP BY");
		expected.AppendLine("t.name");
		expected.AppendLine("HAVING");
		expected.Append("COUNT(t.name) > 1");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
