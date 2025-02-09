using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

internal sealed class ConditionTestAssert(test_table t)
{
	private readonly test_table _t = t;

	public  void Equal(
		ICondition testCondition,
		string expectedSql,
		int expectedBindCount = 0)
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.WHERE(testCondition)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("WHERE");
		expected.Append(expectedSql);

		Assert.Equal(expected.ToString(), sql.Statement);
		Assert.Equal(expectedBindCount, sql.Parameters.Count);
	}
}
