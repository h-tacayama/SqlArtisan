using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class LogicalConditionTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public LogicalConditionTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void AND_MultipleConditions_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("(");
		expected.Append("\"t\".code = 1");
		expected.Append(") ");
		expected.Append("AND ");
		expected.Append("(");
		expected.Append("\"t\".code = 2");
		expected.Append(")");

		_assert.Equal(
			AND(_t.code == L(1), _t.code == L(2)),
			expected.ToString());
	}

	[Fact]
	public void OR_MultipleConditions_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("(");
		expected.Append("\"t\".code = 1");
		expected.Append(") ");
		expected.Append("OR ");
		expected.Append("(");
		expected.Append("\"t\".code = 2");
		expected.Append(")");

		_assert.Equal(
			OR(_t.code == L(1), _t.code == L(2)),
			expected.ToString());
	}

	[Fact]
	public void AND_WithNestedORConditions_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("(");
		expected.Append("(");
		expected.Append("\"t\".code = 1");
		expected.Append(") ");
		expected.Append("OR ");
		expected.Append("(");
		expected.Append("\"t\".code = 2");
		expected.Append(")");
		expected.Append(") ");
		expected.Append("AND ");
		expected.Append("(");
		expected.Append("(");
		expected.Append("\"t\".code = 3");
		expected.Append(") ");
		expected.Append("OR ");
		expected.Append("(");
		expected.Append("\"t\".code = 4");
		expected.Append(")");
		expected.Append(")");

		_assert.Equal(
			AND(
				OR(_t.code == L(1), _t.code == L(2)),
				OR(_t.code == L(3), _t.code == L(4))),
			expected.ToString());
	}

	[Fact]
	public void OR_WithNestedANDConditions_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("(");
		expected.Append("(");
		expected.Append("\"t\".code = 1");
		expected.Append(") ");
		expected.Append("AND ");
		expected.Append("(");
		expected.Append("\"t\".code = 2");
		expected.Append(")");
		expected.Append(") ");
		expected.Append("OR ");
		expected.Append("(");
		expected.Append("(");
		expected.Append("\"t\".code = 3");
		expected.Append(") ");
		expected.Append("AND ");
		expected.Append("(");
		expected.Append("\"t\".code = 4");
		expected.Append(")");
		expected.Append(")");

		_assert.Equal(
			OR(
				AND(_t.code == L(1), _t.code == L(2)),
				AND(_t.code == L(3), _t.code == L(4))),
			expected.ToString());
	}

	[Fact]
	public void NOT_SingleCondition_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("NOT ");
		expected.Append("(");
		expected.Append("\"t\".code = 1");
		expected.Append(")");

		_assert.Equal(
			NOT(_t.code == L(1)),
			expected.ToString());
	}
}
