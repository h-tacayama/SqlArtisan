using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

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
	public void Character_IN_Subquery_With_Parameter()
	{
		StringBuilder expected = new();
		expected.AppendLine("(");
		expected.AppendLine("t.code = :P_0");
		expected.AppendLine(")");
		expected.AppendLine("AND");
		expected.AppendLine("(");
		expected.AppendLine("t.name IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("s.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table s");
		expected.AppendLine("WHERE");
		expected.AppendLine("s.code = :P_1");
		expected.AppendLine(")");
		expected.AppendLine(")");
		expected.AppendLine("AND");
		expected.AppendLine("(");
		expected.AppendLine("t.code = :P_2");
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
	public void Character_NOT_IN_Subquery()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.name NOT IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("s.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table s");
		expected.Append(")");

		_assert.Equal(
			_t.name.NOT_IN(SELECT(_s.name).FROM(_s)),
			expected.ToString());
	}

	[Fact]
	public void DateTime_IN_Subquery()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.created_at IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("s.created_at");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table s");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.IN(SELECT(_s.created_at).FROM(_s)),
			expected.ToString());
	}

	[Fact]
	public void DateTime_NOT_IN_Subquery()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.created_at NOT IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("s.created_at");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table s");
		expected.Append(")");

		_assert.Equal(
			_t.created_at.NOT_IN(SELECT(_s.created_at).FROM(_s)),
			expected.ToString());
	}

	[Fact]
	public void Numeric_IN_Subquery()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.code IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("s.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table s");
		expected.Append(")");

		_assert.Equal(
			_t.code.IN(SELECT(_s.code).FROM(_s)),
			expected.ToString());
	}

	[Fact]
	public void Numeric_NOT_IN_Subquery()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.code NOT IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("s.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table s");
		expected.Append(")");

		_assert.Equal(
			_t.code.NOT_IN(SELECT(_s.code).FROM(_s)),
			expected.ToString());
	}
}
