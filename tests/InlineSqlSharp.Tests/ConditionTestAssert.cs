using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

internal sealed class ConditionTestAssert(test_table t)
{
	private readonly test_table _t = t;

	internal void Equal(
		ICondition testCondition,
		string expectedSql,
		int expectedBindCount = 0)
	{
		SqlStatement sql =
			SELECT(_t.name)
			.FROM(_t)
			.WHERE(testCondition)
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.name ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("WHERE ");
		expected.Append(expectedSql);

		Assert.Equal(expected.ToString(), sql.Text);
		Assert.Equal(expectedBindCount, sql.Parameters.Count);
	}
}
