using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class DynamicConditionTest
{
	private test_table _t = new("t");

	[Fact]
	public void AddConditionIf_Add() =>
		TestImpl(AddConditionIf(true, _t.code == L(1)), "t.code = 1");

	[Fact]
	public void AddConditionIf_Not_Add() =>
		TestImpl(AddConditionIf(false, _t.code == L(1)), string.Empty);

	private void TestImpl(
		ICondition testCondition,
		string expectedSql)
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
	}
}
