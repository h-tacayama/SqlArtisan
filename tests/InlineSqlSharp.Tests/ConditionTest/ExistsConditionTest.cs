using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class ExistsConditionTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public ExistsConditionTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void EXISTS_SELECT()
	{
		StringBuilder expected = new();
		expected.AppendLine("EXISTS");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("1");
		expected.Append(")");

		_assert.Equal(
			EXISTS(SELECT(L(1))),
			expected.ToString());
	}

	[Fact]
	public void EXISTS_SELECT_FROM()
	{
		StringBuilder expected = new();
		expected.AppendLine("EXISTS");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.Append(")");

		_assert.Equal(
			EXISTS(SELECT(_t.code).FROM(_t)),
			expected.ToString());
	}

	[Fact]
	public void EXISTS_SELECT_FROM_WHERE()
	{
		StringBuilder expected = new();
		expected.AppendLine("(");
		expected.AppendLine("t.code = :P_0");
		expected.AppendLine(")");
		expected.AppendLine("AND");
		expected.AppendLine("(");
		expected.AppendLine("EXISTS");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("WHERE");
		expected.AppendLine("t.name = :P_1");
		expected.AppendLine(")");
		expected.AppendLine(")");
		expected.AppendLine("AND");
		expected.AppendLine("(");
		expected.AppendLine("t.code = :P_2");
		expected.Append(")");

		// Check if parameter indexes are correctly incremented
		_assert.Equal(
			AND(
				_t.code == P(1),
				EXISTS(SELECT(_t.code).FROM(_t).WHERE(_t.name == P("a"))),
				_t.code == P(2)),
			expected.ToString(),
			3);
	}

	[Fact]
	public void NOT_EXISTS_SELECT_FROM_WHERE()
	{
		StringBuilder expected = new();
		expected.AppendLine("NOT EXISTS");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("WHERE");
		expected.AppendLine("t.code = :P_0");
		expected.Append(")");

		_assert.Equal(
			NOT_EXISTS(SELECT(_t.code).FROM(_t).WHERE(_t.code == P(1))),
			expected.ToString(),
			1);
	}
}
