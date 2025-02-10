using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class GroupByTest
{
	private test_table _t = new("t");

	[Fact]
	public void GROUP_BY_Single()
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.GROUP_BY(_t.name)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("GROUP BY");
		expected.Append("t.name");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void GROUP_BY_Multi()
	{
		SqlCommand sql =
			SELECT(
				_t.code,
				_t.name)
			.FROM(_t)
			.GROUP_BY(
				_t.code,
				_t.name)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine(", t.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("GROUP BY");
		expected.AppendLine("t.code");
		expected.Append(", t.name");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
