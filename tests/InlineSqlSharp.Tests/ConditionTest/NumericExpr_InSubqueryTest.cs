using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class NumericExpr_InSubqueryTest
{
	private readonly test_table _t;
	private readonly test_table _s;
	private readonly ConditionTestAssert _assert;

	public NumericExpr_InSubqueryTest()
	{
		_t = new test_table("t");
		_s = new test_table("s");
		_assert = new(_t);
	}

	[Fact]
	public void NumericExpr_IN_Subquery_CorrectSql()
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
	public void NumericExpr_NOT_IN_Subquery_CorrectSql()
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
