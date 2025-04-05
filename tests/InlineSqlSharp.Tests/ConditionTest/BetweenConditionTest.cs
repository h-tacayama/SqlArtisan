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
	public void BETWEEN_CharacterLiterals_CorrectSql() =>
		_assert.Equal(
			_t.name.BETWEEN(L("a"), L("z")),
			"\"t\".name BETWEEN 'a' AND 'z'");

	[Fact]
	public void BETWEEN_CharacterLiteralAndString_CorrectSql() =>
		_assert.Equal(
			_t.name.BETWEEN(L("a"), "z"),
			"\"t\".name BETWEEN 'a' AND :0",
			1, "z");

	[Fact]
	public void BETWEEN_CharacterStringAndLiteral_CorrectSql() =>
		_assert.Equal(
			_t.name.BETWEEN("a", L("z")),
			"\"t\".name BETWEEN :0 AND 'z'",
			1, "a");

	[Fact]
	public void BETWEEN_CharacterStrings_CorrectSql() =>
		_assert.Equal(
			_t.name.BETWEEN("a", "z"),
			"\"t\".name BETWEEN :0 AND :1",
			2, "a", "z");

	[Fact]
	public void NOT_BETWEEN_CharacterLiterals_CorrectSql() =>
		_assert.Equal(
			_t.name.NOT_BETWEEN(L("a"), L("z")),
			"\"t\".name NOT BETWEEN 'a' AND 'z'");

	[Fact]
	public void NOT_BETWEEN_CharacterLiteralAndString_CorrectSql() =>
		_assert.Equal(
			_t.name.NOT_BETWEEN(L("a"), "z"),
			"\"t\".name NOT BETWEEN 'a' AND :0",
			1, "z");

	[Fact]
	public void NOT_BETWEEN_CharacterStringAndLiteral_CorrectSql() =>
		_assert.Equal(
			_t.name.NOT_BETWEEN("a", L("z")),
			"\"t\".name NOT BETWEEN :0 AND 'z'",
			1, "a");

	[Fact]
	public void NOT_BETWEEN_CharacterStrings_CorrectSql() =>
		_assert.Equal(
			_t.name.NOT_BETWEEN("a", "z"),
			"\"t\".name NOT BETWEEN :0 AND :1",
			2, "a", "z");

	[Fact]
	public void BETWEEN_DateTimeColumns_CorrectSql() =>
		_assert.Equal(
			_t.created_at.BETWEEN(_t.created_at, _t.created_at),
			"\"t\".created_at BETWEEN \"t\".created_at AND \"t\".created_at");

	[Fact]
	public void BETWEEN_DateTimeColumnAndValue_CorrectSql() =>
		_assert.Equal(
			_t.created_at.BETWEEN(_t.created_at, new DateTime(2001, 2, 3)),
			"\"t\".created_at BETWEEN \"t\".created_at AND :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void BETWEEN_DateTimeValueAndColumn_CorrectSql() =>
		_assert.Equal(
			_t.created_at.BETWEEN(new DateTime(2001, 2, 3), _t.created_at),
			"\"t\".created_at BETWEEN :0 AND \"t\".created_at",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void BETWEEN_DateTimeValues_CorrectSql() =>
		_assert.Equal(
			_t.created_at.BETWEEN(new DateTime(2001, 2, 3), new DateTime(2004, 5, 6)),
			"\"t\".created_at BETWEEN :0 AND :1",
			2, new DateTime(2001, 2, 3), new DateTime(2004, 5, 6));

	[Fact]
	public void NOT_BETWEEN_DateTimeColumns_CorrectSql() =>
		_assert.Equal(
			_t.created_at.NOT_BETWEEN(_t.created_at, _t.created_at),
			"\"t\".created_at NOT BETWEEN \"t\".created_at AND \"t\".created_at");

	[Fact]
	public void NOT_BETWEEN_DateTimeColumnAndValue_CorrectSql() =>
		_assert.Equal(
			_t.created_at.NOT_BETWEEN(_t.created_at, new DateTime(2001, 2, 3)),
			"\"t\".created_at NOT BETWEEN \"t\".created_at AND :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void NOT_BETWEEN_DateTimeValueAndColumn_CorrectSql() =>
		_assert.Equal(
			_t.created_at.NOT_BETWEEN(new DateTime(2001, 2, 3), _t.created_at),
			"\"t\".created_at NOT BETWEEN :0 AND \"t\".created_at",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void NOT_BETWEEN_DateTimeValues_CorrectSql() =>
		_assert.Equal(
			_t.created_at.NOT_BETWEEN(new DateTime(2001, 2, 3), new DateTime(2004, 5, 6)),
			"\"t\".created_at NOT BETWEEN :0 AND :1",
			2, new DateTime(2001, 2, 3), new DateTime(2004, 5, 6));

	[Fact]
	public void BETWEEN_NumericLiterals_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(L(1), L(10)), "\"t\".code BETWEEN 1 AND 10");

	[Fact]
	public void NOT_BETWEEN_NumericLiterals_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), L(10)), "\"t\".code NOT BETWEEN 1 AND 10");
}
