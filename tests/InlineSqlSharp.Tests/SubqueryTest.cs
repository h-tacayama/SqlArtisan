using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class SubqueryTest
{
	private readonly test_table _t;
	private readonly test_table _s;
	private readonly test_table _r;
	private readonly ConditionTestAssert _assert;

	public SubqueryTest()
	{
		_t = new test_table("t");
		_s = new test_table("s");
		_r = new test_table("r");
		_assert = new(_t);
	}

	[Fact]
	public void Subquery_SELECT()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.code IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("s.code");
		expected.Append(")");

		_assert.Equal(
			_t.code.IN(SELECT(_s.code)),
			expected.ToString());
	}

	[Fact]
	public void Subquery_SELECT_FROM()
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
	public void Subquery_SELECT_DISTINCT_FROM()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.code IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("DISTINCT");
		expected.AppendLine("s.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table s");
		expected.Append(")");

		_assert.Equal(
			_t.code.IN(SELECT(DISTINCT, _s.code).FROM(_s)),
			expected.ToString());
	}

	[Fact]
	public void Subquery_SELECT_FROM_WHERE()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.code IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("s.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table s");
		expected.AppendLine("WHERE");
		expected.AppendLine("s.code > 1");
		expected.Append(")");

		_assert.Equal(
			_t.code.IN(SELECT(_s.code).FROM(_s).WHERE(_s.code > L(1))),
			expected.ToString());
	}

	[Fact]
	public void Subquery_SELECT_FROM_INNER_JOIN_ON()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.code IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("s.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table s");
		expected.AppendLine("INNER JOIN");
		expected.AppendLine("test_table r");
		expected.AppendLine("ON");
		expected.AppendLine("s.code = r.code");
		expected.Append(")");

		_assert.Equal(
			_t.code.IN(SELECT(_s.code).FROM(_s).INNER_JOIN(_r).ON(_s.code == _r.code)),
			expected.ToString());
	}

	[Fact]
	public void Subquery_SELECT_Hints()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.code IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("/*+ ANY HINT */");
		expected.AppendLine("s.code");
		expected.Append(")");

		_assert.Equal(
			_t.code.IN(SELECT(HINTS("/*+ ANY HINT */"), _s.code)),
			expected.ToString());
	}

	[Fact]
	public void Subquery_SELECT_Hints_DISTINCT()
	{
		StringBuilder expected = new();
		expected.AppendLine("t.code IN");
		expected.AppendLine("(");
		expected.AppendLine("SELECT");
		expected.AppendLine("/*+ ANY HINT */");
		expected.AppendLine("DISTINCT");
		expected.AppendLine("s.code");
		expected.Append(")");

		_assert.Equal(
			_t.code.IN(SELECT(HINTS("/*+ ANY HINT */"), DISTINCT, _s.code)),
			expected.ToString());
	}
}
