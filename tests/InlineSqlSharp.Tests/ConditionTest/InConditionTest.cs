using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class InConditionTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public InConditionTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void Character_IN_Single()
	{
		StringBuilder expected = new();
		expected.Append("t.name IN ");
		expected.Append("(");
		expected.Append("'a'");
		expected.Append(")");

		_assert.Equal(_t.name.IN(L("a")), expected.ToString());
	}

	[Fact]
	public void Character_IN_Multi()
	{
		StringBuilder expected = new();
		expected.Append("t.name IN ");
		expected.Append("(");
		expected.Append(":P_0, ");
		expected.Append(":P_1, ");
		expected.Append(":P_2");
		expected.Append(")");

		_assert.Equal(
			_t.name.IN(P("a"), P("b"), P("c")),
			expected.ToString(),
			3);
	}

	[Fact]
	public void Character_NOT_IN_Single()
	{
		StringBuilder expected = new();
		expected.Append("t.name NOT IN ");
		expected.Append("(");
		expected.Append("'a'");
		expected.Append(")");

		_assert.Equal(_t.name.NOT_IN(L("a")), expected.ToString());
	}

	[Fact]
	public void DateTime_IN_Single()
	{
		StringBuilder expected = new();
		expected.Append("t.created_at IN ");
		expected.Append("(");
		expected.Append(":P_0");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.IN(P(new DateTime(2001, 2, 3))),
			expected.ToString(),
			1);
	}

	[Fact]
	public void DateTime_IN_Multi()
	{
		StringBuilder expected = new();
		expected.Append("t.created_at IN ");
		expected.Append("(");
		expected.Append(":P_0, ");
		expected.Append(":P_1, ");
		expected.Append(":P_2");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.IN(
			P(new DateTime(2001, 2, 3)),
			P(new DateTime(2001, 2, 4)),
			P(new DateTime(2001, 2, 5))),
			expected.ToString(),
			3);
	}

	[Fact]
	public void DateTime_NOT_IN_Single()
	{
		StringBuilder expected = new();
		expected.Append("t.created_at NOT IN ");
		expected.Append("(");
		expected.Append(":P_0");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.NOT_IN(P(new DateTime(2001, 2, 3))),
			expected.ToString(),
			1);
	}

	[Fact]
	public void Numeric_IN_Single()
	{
		StringBuilder expected = new();
		expected.Append("t.code IN ");
		expected.Append("(");
		expected.Append("1");
		expected.Append(")");

		_assert.Equal(_t.code.IN(L(1)), expected.ToString());
	}

	[Fact]
	public void Numeric_IN_Multi()
	{
		StringBuilder expected = new();
		expected.Append("t.code IN ");
		expected.Append("(");
		expected.Append(":P_0, ");
		expected.Append(":P_1, ");
		expected.Append(":P_2");
		expected.Append(")");

		_assert.Equal(_t.code.IN(P(1), P(2), P(3)),
			expected.ToString(),
			3);
	}

	[Fact]
	public void Numeric_NOT_IN_Single()
	{
		StringBuilder expected = new();
		expected.Append("t.code NOT IN ");
		expected.Append("(");
		expected.Append("1");
		expected.Append(")");

		_assert.Equal(_t.code.NOT_IN(L(1)), expected.ToString());
	}
}
