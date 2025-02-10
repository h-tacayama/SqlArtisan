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

		_assert.Equal(
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

		_assert.Equal(
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

		_assert.Equal(
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

		_assert.Equal(
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

		_assert.Equal(
			NOT(_t.code == L(1)),
			expected.ToString());
	}
}
