using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class BetweenConditionTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public BetweenConditionTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void BETWEEN_CharacterValues_CorrectSql() =>
		_assert.Equal(
			_t.name.BETWEEN(L("a"), L("z")),
			"t.name BETWEEN 'a' AND 'z'");

	[Fact]
	public void NOT_BETWEEN_CharacterValues_CorrectSql() =>
		_assert.Equal(
			_t.name.NOT_BETWEEN(L("a"), L("z")),
			"t.name NOT BETWEEN 'a' AND 'z'");

	[Fact]
	public void BETWEEN_DateTimeValues_CorrectSql() =>
		_assert.Equal(
			_t.created_at.BETWEEN(_t.created_at, _t.created_at),
			"t.created_at BETWEEN t.created_at AND t.created_at");

	[Fact]
	public void NOT_BETWEEN_DateTimeValues_CorrectSql() =>
		_assert.Equal(
			_t.created_at.NOT_BETWEEN(_t.created_at, _t.created_at),
			"t.created_at NOT BETWEEN t.created_at AND t.created_at");

	[Fact]
	public void BETWEEN_NumericValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(L(1), L(10)), "t.code BETWEEN 1 AND 10");

	[Fact]
	public void NOT_BETWEEN_NumericValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), L(10)), "t.code NOT BETWEEN 1 AND 10");
}
