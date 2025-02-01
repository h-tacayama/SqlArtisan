using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class NullConditionTest
{
	private test_table _t = new("t");

	[Fact]
	public void Character_IS_NULL() =>
		TestImpl(_t.name.IS_NULL,"t.name IS NULL");

	[Fact]
	public void Character_IS_NOT_NULL() =>
		TestImpl(_t.name.IS_NOT_NULL, "t.name IS NOT NULL");

	[Fact]
	public void DateTime_IS_NULL() =>
		TestImpl(_t.created_at.IS_NULL, "t.created_at IS NULL");

	[Fact]
	public void DateTime_IS_NOT_NULL() =>
		TestImpl(_t.created_at.IS_NOT_NULL, "t.created_at IS NOT NULL");

	[Fact]
	public void Numeric_IS_NULL() =>
		TestImpl(_t.code.IS_NULL, "t.code IS NULL");

	[Fact]
	public void Numeric_IS_NOT_NULL() =>
		TestImpl(_t.code.IS_NOT_NULL, "t.code IS NOT NULL");

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
