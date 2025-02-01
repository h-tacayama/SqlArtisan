using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class LogicalConditionTest
{
	private test_table _t = new("t");

	[Fact]
	public void And()
	{
		StringBuilder expected = new();
		expected.AppendLine("(");
		expected.AppendLine("t.code = 1");
		expected.AppendLine(")");
		expected.AppendLine("AND");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 2");
		expected.Append(")");

		TestImpl(
			AND(_t.code == L(1), _t.code == L(2)),
			expected.ToString());
	}

	[Fact]
	public void Or()
	{
		StringBuilder expected = new();
		expected.AppendLine("(");
		expected.AppendLine("t.code = 1");
		expected.AppendLine(")");
		expected.AppendLine("OR");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 2");
		expected.Append(")");

		TestImpl(
			OR(_t.code == L(1), _t.code == L(2)),
			expected.ToString());
	}

	[Fact]
	public void And_Or()
	{
		StringBuilder expected = new();
		expected.AppendLine("(");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 1");
		expected.AppendLine(")");
		expected.AppendLine("OR");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 2");
		expected.AppendLine(")");
		expected.AppendLine(")");
		expected.AppendLine("AND");
		expected.AppendLine("(");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 3");
		expected.AppendLine(")");
		expected.AppendLine("OR");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 4");
		expected.AppendLine(")");
		expected.Append(")");

		TestImpl(
			AND(
				OR(_t.code == L(1), _t.code == L(2)),
				OR(_t.code == L(3), _t.code == L(4))),
			expected.ToString());
	}

	[Fact]
	public void Or_And()
	{
		StringBuilder expected = new();
		expected.AppendLine("(");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 1");
		expected.AppendLine(")");
		expected.AppendLine("AND");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 2");
		expected.AppendLine(")");
		expected.AppendLine(")");
		expected.AppendLine("OR");
		expected.AppendLine("(");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 3");
		expected.AppendLine(")");
		expected.AppendLine("AND");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 4");
		expected.AppendLine(")");
		expected.Append(")");

		TestImpl(
			OR(
				AND(_t.code == L(1), _t.code == L(2)),
				AND(_t.code == L(3), _t.code == L(4))),
			expected.ToString());
	}

	[Fact]
	public void Not()
	{
		StringBuilder expected = new();
		expected.AppendLine("NOT");
		expected.AppendLine("(");
		expected.AppendLine("t.code = 1");
		expected.Append(")");

		TestImpl(
			NOT(_t.code == L(1)),
			expected.ToString());
	}

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
