using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void INSTR_BasicPattern_CorrectSql()
	{
		SqlCommand sql =
			SELECT(INSTR(_t.name, L("abc")))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("INSTR(t.name, 'abc')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void INSTR_WithPosition_CorrectSql()
	{
		SqlCommand sql =
			SELECT(
				INSTR(_t.name, L("abc"), L(2)),
				INSTR(_t.name, L("abc"), L(1)),
				INSTR(_t.name, L("abc"), L(-1)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("INSTR(t.name, 'abc', 2), ");
		expected.Append("INSTR(t.name, 'abc', 1), ");
		expected.Append("INSTR(t.name, 'abc', -1)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void INSTR_WithOccurrence_CorrectSql()
	{
		SqlCommand sql =
			SELECT(
				INSTR(_t.name, L("abc"), L(1), L(1)),
				INSTR(_t.name, L("abc"), L(1), L(2)),
				INSTR(_t.name, L("abc"), L(2), L(-1)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("INSTR(t.name, 'abc', 1, 1), ");
		expected.Append("INSTR(t.name, 'abc', 1, 2), ");
		expected.Append("INSTR(t.name, 'abc', 2, -1)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
