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
	public void Equal_CharacterValues_CorrectSql() =>
		_assert.Equal(_t.name == P("abc"), "\"t\".name = :0", 1);

	[Fact]
	public void NotEqual_CharacterValues_CorrectSql() =>
		_assert.Equal(_t.name != P("abc"), "\"t\".name <> :0", 1);

	[Fact]
	public void LessThan_CharacterValues_CorrectSql() =>
		_assert.Equal(_t.name < P("abc"), "\"t\".name < :0", 1);

	[Fact]
	public void GreaterThan_CharacterValues_CorrectSql() =>
		_assert.Equal(_t.name > P("abc"), "\"t\".name > :0", 1);

	[Fact]
	public void LessEqual_CharacterValues_CorrectSql() =>
		_assert.Equal(_t.name <= P("abc"), "\"t\".name <= :0", 1);

	[Fact]
	public void GreaterEqual_CharacterValues_CorrectSql() =>
		_assert.Equal(_t.name >= P("abc"), "\"t\".name >= :0", 1);

	[Fact]
	public void Equal_DateTimeValues_CorrectSql() =>
		_assert.Equal(_t.created_at == P(new DateTime(2001, 2, 3)), "\"t\".created_at = :0", 1);

	[Fact]
	public void NotEqual_DateTimeValues_CorrectSql() =>
		_assert.Equal(_t.created_at != P(new DateTime(2001, 2, 3)), "\"t\".created_at <> :0", 1);

	[Fact]
	public void LessThan_DateTimeValues_CorrectSql() =>
		_assert.Equal(_t.created_at < P(new DateTime(2001, 2, 3)), "\"t\".created_at < :0", 1);

	[Fact]
	public void GreaterThan_DateTimeValues_CorrectSql() =>
		_assert.Equal(_t.created_at > P(new DateTime(2001, 2, 3)), "\"t\".created_at > :0", 1);

	[Fact]
	public void LessEqual_DateTimeValues_CorrectSql() =>
		_assert.Equal(_t.created_at <= P(new DateTime(2001, 2, 3)), "\"t\".created_at <= :0", 1);

	[Fact]
	public void GreaterEqual_DateTimeValues_CorrectSql() =>
		_assert.Equal(_t.created_at >= P(new DateTime(2001, 2, 3)), "\"t\".created_at >= :0", 1);

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
