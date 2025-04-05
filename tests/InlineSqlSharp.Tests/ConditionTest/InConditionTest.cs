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
	public void IN_CharacterSingleLiteral_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".name IN ");
		expected.Append("(");
		expected.Append("'a'");
		expected.Append(")");

		_assert.Equal(_t.name.IN(L("a")), expected.ToString());
	}

	[Fact]
	public void IN_CharacterMultipleStrings_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".name IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		_assert.Equal(
			_t.name.IN("a", "b", "c"),
			expected.ToString(),
			3, "a", "b", "c");
	}

	[Fact]
	public void NOT_IN_CharacterSingleLiteral_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".name NOT IN ");
		expected.Append("(");
		expected.Append("'a'");
		expected.Append(")");

		_assert.Equal(_t.name.NOT_IN(L("a")), expected.ToString());
	}

	[Fact]
	public void NOT_IN_CharacterMultipleStrings_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".name NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		_assert.Equal(
			_t.name.NOT_IN("a", "b", "c"),
			expected.ToString(),
			3, "a", "b", "c");
	}

	[Fact]
	public void IN_DateTimeSingleParameter_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".created_at IN ");
		expected.Append("(");
		expected.Append(":0");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.IN(P(new DateTime(2001, 2, 3))),
			expected.ToString(),
			1, new DateTime(2001, 2, 3));
	}

	[Fact]
	public void IN_DateTimeMultipleValues_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".created_at IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.IN(
			new DateTime(2001, 2, 3),
			new DateTime(2001, 2, 4),
			new DateTime(2001, 2, 5)),
			expected.ToString(),
			3,
			new DateTime(2001, 2, 3),
			new DateTime(2001, 2, 4),
			new DateTime(2001, 2, 5));
	}

	[Fact]
	public void NOT_IN_DateTimeSingleParameter_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".created_at NOT IN ");
		expected.Append("(");
		expected.Append(":0");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.NOT_IN(P(new DateTime(2001, 2, 3))),
			expected.ToString(),
			1, new DateTime(2001, 2, 3));
	}

	[Fact]
	public void NOT_IN_DateTimeMultipleValues_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".created_at NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.NOT_IN(
			new DateTime(2001, 2, 3),
			new DateTime(2001, 2, 4),
			new DateTime(2001, 2, 5)),
			expected.ToString(),
			3,
			new DateTime(2001, 2, 3),
			new DateTime(2001, 2, 4),
			new DateTime(2001, 2, 5));
	}

	[Fact]
	public void IN_NumericValues_WithSingleValue_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append("1");
		expected.Append(")");

		_assert.Equal(_t.code.IN(L(1)), expected.ToString());
	}

	[Fact]
	public void IN_NumericValues_WithMultipleValues_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		_assert.Equal(_t.code.IN(P(1), P(2), P(3)),
			expected.ToString(),
			3);
	}

	[Fact]
	public void NOT_IN_NumericValues_WithSingleValue_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append("1");
		expected.Append(")");

		_assert.Equal(_t.code.NOT_IN(L(1)), expected.ToString());
	}
}
