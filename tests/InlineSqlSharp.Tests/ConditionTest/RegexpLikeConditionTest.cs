using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class RegexpLikeConditionTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public RegexpLikeConditionTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void REGEXP_LIKE_NoOptions_CorrectSql() =>
		_assert.Equal(
			REGEXP_LIKE(_t.name, L("[2-5]")),
			"REGEXP_LIKE(t.name, '[2-5]')");

	[Fact]
	public void REGEXP_LIKE_CaseSensitive_CorrectSql() =>
		_assert.Equal(
			REGEXP_LIKE(_t.name, L("[2-5]"), RegexpOptions.CaseSensitive),
			"REGEXP_LIKE(t.name, '[2-5]', 'c')");

	[Fact]
	public void REGEXP_LIKE_CaseInsensitive_CorrectSql() =>
		_assert.Equal(
			REGEXP_LIKE(_t.name, L("[2-5]"), RegexpOptions.CaseInsensitive),
			"REGEXP_LIKE(t.name, '[2-5]', 'i')");

	[Fact]
	public void REGEXP_LIKE_MultipleLines_CorrectSql() =>
		_assert.Equal(
			REGEXP_LIKE(_t.name, L("[2-5]"), RegexpOptions.MultipleLines),
			"REGEXP_LIKE(t.name, '[2-5]', 'm')");

	[Fact]
	public void REGEXP_LIKE_NewLine_CorrectSql() =>
		_assert.Equal(
			REGEXP_LIKE(_t.name, L("[2-5]"), RegexpOptions.NewLine),
			"REGEXP_LIKE(t.name, '[2-5]', 'n')");

	[Fact]
	public void REGEXP_LIKE_ExcludingWhiteSpace_CorrectSql() =>
		_assert.Equal(
			REGEXP_LIKE(_t.name, L("[2-5]"), RegexpOptions.ExcludingWhiteSpace),
			"REGEXP_LIKE(t.name, '[2-5]', 'x')");

	[Fact]
	public void REGEXP_LIKE_AllOptions_CorrectSql() =>
		_assert.Equal(
			REGEXP_LIKE(_t.name, L("[2-5]"),
				RegexpOptions.CaseSensitive
				| RegexpOptions.CaseInsensitive
				| RegexpOptions.MultipleLines
				| RegexpOptions.NewLine
				| RegexpOptions.ExcludingWhiteSpace),
			"REGEXP_LIKE(t.name, '[2-5]', 'cimnx')");
}
