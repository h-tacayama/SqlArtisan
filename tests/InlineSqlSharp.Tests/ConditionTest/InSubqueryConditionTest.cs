using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class InSubqueryConditionTest
{
	private readonly test_table _t;
	private readonly test_table _s;
	private readonly ConditionTestAssert _assert;

	public InSubqueryConditionTest()
	{
		_t = new test_table("t");
		_s = new test_table("s");
		_assert = new(_t);
	}

	[Fact]
	public void IN_CharacterValues_WithSubquery_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("(");
		expected.Append("\"t\".code = :0");
		expected.Append(") ");
		expected.Append("AND ");
		expected.Append("(");
		expected.Append("\"t\".name IN ");
		expected.Append("(");
		expected.Append("SELECT ");
		expected.Append("\"s\".name ");
		expected.Append("FROM ");
		expected.Append("test_table \"s\" ");
		expected.Append("WHERE ");
		expected.Append("\"s\".code = :1");
		expected.Append(")");
		expected.Append(") ");
		expected.Append("AND ");
		expected.Append("(");
		expected.Append("\"t\".code = :2");
		expected.Append(")");

		_assert.Equal(
			AND(
				_t.code == P(1),
				_t.name.IN(SELECT(_s.name).FROM(_s).WHERE(_s.code == P(2))),
				_t.code == P(3)),
			expected.ToString(),
			3);
	}

	[Fact]
	public void NOT_IN_CharacterValues_WithSubquery_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".name NOT IN ");
		expected.Append("(");
		expected.Append("SELECT ");
		expected.Append("\"s\".name ");
		expected.Append("FROM ");
		expected.Append("test_table \"s\"");
		expected.Append(")");

		_assert.Equal(
			_t.name.NOT_IN(SELECT(_s.name).FROM(_s)),
			expected.ToString());
	}

	[Fact]
	public void IN_DateTimeValues_WithSubquery_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".created_at IN ");
		expected.Append("(");
		expected.Append("SELECT ");
		expected.Append("\"s\".created_at ");
		expected.Append("FROM ");
		expected.Append("test_table \"s\"");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.IN(SELECT(_s.created_at).FROM(_s)),
			expected.ToString());
	}

	[Fact]
	public void NOT_IN_DateTimeValues_WithSubquery_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".created_at NOT IN ");
		expected.Append("(");
		expected.Append("SELECT ");
		expected.Append("\"s\".created_at ");
		expected.Append("FROM ");
		expected.Append("test_table \"s\"");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.NOT_IN(SELECT(_s.created_at).FROM(_s)),
			expected.ToString());
	}

	[Fact]
	public void IN_NumericValues_WithSubquery_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append("SELECT ");
		expected.Append("\"s\".code ");
		expected.Append("FROM ");
		expected.Append("test_table \"s\"");
		expected.Append(")");

		_assert.Equal(
			_t.code.IN(SELECT(_s.code).FROM(_s)),
			expected.ToString());
	}

	[Fact]
	public void NOT_IN_NumericValues_WithSubquery_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append("SELECT ");
		expected.Append("\"s\".code ");
		expected.Append("FROM ");
		expected.Append("test_table \"s\"");
		expected.Append(")");

		_assert.Equal(
			_t.code.NOT_IN(SELECT(_s.code).FROM(_s)),
			expected.ToString());
	}
}
