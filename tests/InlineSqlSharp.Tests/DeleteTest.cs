using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class DeleteTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void DELETE_FROM_Test()
	{
		SqlCommand sql =
			DELETE_FROM(_t)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("DELETE FROM");
		expected.Append("test_table t");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void DELETE_FROM_WHERE()
	{
		SqlCommand sql =
			DELETE_FROM(_t)
			.WHERE(_t.code == L(1))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("DELETE FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("WHERE");
		expected.Append("t.code = 1");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
