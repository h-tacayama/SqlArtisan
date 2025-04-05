using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class ComparisonConditionTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public ComparisonConditionTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void Equal_CharacterColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.name == L("abc"), "\"t\".name = 'abc'");

	[Fact]
	public void NotEqual_CharacterColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.name != L("abc"), "\"t\".name <> 'abc'");

	[Fact]
	public void LessThan_CharacterColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.name < L("abc"), "\"t\".name < 'abc'");

	[Fact]
	public void GreaterThan_CharacterColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.name > L("abc"), "\"t\".name > 'abc'");

	[Fact]
	public void LessEqual_CharacterColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.name <= L("abc"), "\"t\".name <= 'abc'");

	[Fact]
	public void GreaterEqual_CharacterColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.name >= L("abc"), "\"t\".name >= 'abc'");

	[Fact]
	public void Equal_CharacterColumnAndString_CorrectSql() =>
		_assert.Equal(_t.name == "abc", "\"t\".name = :0", 1, "abc");

	[Fact]
	public void NotEqual_CharacterColumnAndString_CorrectSql() =>
		_assert.Equal(_t.name != "abc", "\"t\".name <> :0", 1, "abc");

	[Fact]
	public void LessThan_CharacterColumnAndString_CorrectSql() =>
		_assert.Equal(_t.name < "abc", "\"t\".name < :0", 1, "abc");

	[Fact]
	public void GreaterThan_CharacterColumnAndString_CorrectSql() =>
		_assert.Equal(_t.name > "abc", "\"t\".name > :0", 1, "abc");

	[Fact]
	public void LessEqual_CharacterColumnAndString_CorrectSql() =>
		_assert.Equal(_t.name <= "abc", "\"t\".name <= :0", 1, "abc");

	[Fact]
	public void GreaterEqual_CharacterColumnAndString_CorrectSql() =>
		_assert.Equal(_t.name >= "abc", "\"t\".name >= :0", 1, "abc");

	[Fact]
	public void Equal_DateTimeColumnAndParameter_CorrectSql() =>
		_assert.Equal(_t.created_at == P(new DateTime(2001, 2, 3)),
			"\"t\".created_at = :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void NotEqual_DateTimeColumnAndParameter_CorrectSql() =>
		_assert.Equal(_t.created_at != P(new DateTime(2001, 2, 3)),
			"\"t\".created_at <> :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void LessThan_DateTimeColumnAndParameter_CorrectSql() =>
		_assert.Equal(_t.created_at < P(new DateTime(2001, 2, 3)),
			"\"t\".created_at < :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void GreaterThan_DateTimeColumnAndParameter_CorrectSql() =>
		_assert.Equal(_t.created_at > P(new DateTime(2001, 2, 3)),
			"\"t\".created_at > :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void LessEqual_DateTimeColumnAndParameter_CorrectSql() =>
		_assert.Equal(_t.created_at <= P(new DateTime(2001, 2, 3)),
			"\"t\".created_at <= :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void GreaterEqual_DateTimeColumnAndParameter_CorrectSql() =>
		_assert.Equal(_t.created_at >= P(new DateTime(2001, 2, 3)),
			"\"t\".created_at >= :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void Equal_DateTimeColumnAndValue_CorrectSql() =>
		_assert.Equal(_t.created_at == new DateTime(2001, 2, 3),
			"\"t\".created_at = :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void NotEqual_DateTimeColumnAndValue_CorrectSql() =>
		_assert.Equal(_t.created_at != new DateTime(2001, 2, 3),
			"\"t\".created_at <> :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void LessThan_DateTimeColumnAndValue_CorrectSql() =>
		_assert.Equal(_t.created_at < new DateTime(2001, 2, 3),
			"\"t\".created_at < :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void GreaterThan_DateTimeColumnAndValue_CorrectSql() =>
		_assert.Equal(_t.created_at > new DateTime(2001, 2, 3),
			"\"t\".created_at > :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void LessEqual_DateTimeColumnAndValue_CorrectSql() =>
		_assert.Equal(_t.created_at <= new DateTime(2001, 2, 3),
			"\"t\".created_at <= :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void GreaterEqual_DateTimeColumnAndValue_CorrectSql() =>
		_assert.Equal(_t.created_at >= new DateTime(2001, 2, 3),
			"\"t\".created_at >= :0",
			1, new DateTime(2001, 2, 3));

	[Fact]
	public void Equal_NumericValues_CorrectSql() =>
		_assert.Equal(_t.code == P(1), "\"t\".code = :0", 1);

	[Fact]
	public void NotEqual_NumericValues_CorrectSql() =>
		_assert.Equal(_t.code != P(1), "\"t\".code <> :0", 1);

	[Fact]
	public void LessThan_NumericValues_CorrectSql() =>
		_assert.Equal(_t.code < P(1), "\"t\".code < :0", 1);

	[Fact]
	public void GreaterThan_NumericValues_CorrectSql() =>
		_assert.Equal(_t.code > P(1), "\"t\".code > :0", 1);

	[Fact]
	public void LessEqual_NumericValues_CorrectSql() =>
		_assert.Equal(_t.code <= P(1), "\"t\".code <= :0", 1);

	[Fact]
	public void GreaterEqual_NumericValues_CorrectSql() =>
		_assert.Equal(_t.code >= P(1), "\"t\".code >= :0", 1);
}
