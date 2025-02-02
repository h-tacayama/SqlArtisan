namespace InlineSqlSharp.Tests;

public class NullConditionTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public NullConditionTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void Character_IS_NULL() =>
		_assert.Equal(_t.name.IS_NULL,"t.name IS NULL");

	[Fact]
	public void Character_IS_NOT_NULL() =>
		_assert.Equal(_t.name.IS_NOT_NULL, "t.name IS NOT NULL");

	[Fact]
	public void DateTime_IS_NULL() =>
		_assert.Equal(_t.created_at.IS_NULL, "t.created_at IS NULL");

	[Fact]
	public void DateTime_IS_NOT_NULL() =>
		_assert.Equal(_t.created_at.IS_NOT_NULL, "t.created_at IS NOT NULL");

	[Fact]
	public void Numeric_IS_NULL() =>
		_assert.Equal(_t.code.IS_NULL, "t.code IS NULL");

	[Fact]
	public void Numeric_IS_NOT_NULL() =>
		_assert.Equal(_t.code.IS_NOT_NULL, "t.code IS NOT NULL");
}
