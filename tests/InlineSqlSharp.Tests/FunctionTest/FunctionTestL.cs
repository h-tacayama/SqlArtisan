using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void LAST_DAY_DateTimeValue_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LAST_DAY(_t.created_at))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LAST_DAY(\"t\".created_at)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void LEAST_CharacterValues_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LEAST(_t.name, L("test"), _t.name))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LEAST(\"t\".name, 'test', \"t\".name)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void LEAST_DateTimeValues_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LEAST(
				_t.created_at,
				TO_DATE(L("2000/01/01"), L("YYYY/MM/DD")),
				_t.created_at))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LEAST(\"t\".created_at, TO_DATE('2000/01/01', 'YYYY/MM/DD'), \"t\".created_at)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void LEAST_NumericValues_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LEAST(_t.code, L(10), _t.code))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LEAST(\"t\".code, 10, \"t\".code)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void LENGTH_CharacterValue_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LENGTH(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LENGTH(\"t\".name)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void LENGTHB_CharacterValue_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LENGTHB(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LENGTHB(\"t\".name)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void LOWER_CharacterValue_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LOWER(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LOWER(\"t\".name)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void LPAD_CharacterAndLength_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LPAD(_t.name, L(10)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LPAD(\"t\".name, 10)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void LPAD_CharacterLengthAndPadding_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LPAD(_t.name, L(10), L("a")))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LPAD(\"t\".name, 10, 'a')");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void LTRIM_CharacterValue_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LTRIM(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LTRIM(\"t\".name)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void LTRIM_CharacterAndTrimChars_CorrectSql()
	{
		SqlStatement sql =
			SELECT(LTRIM(_t.name, L("a")))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("LTRIM(\"t\".name, 'a')");

		Assert.Equal(expected.ToString(), sql.Text);
	}
}
