using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class LikeConditionTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public LikeConditionTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void LIKE() =>
		_assert.Equal(
			_t.name.LIKE(L("%abc%")),
			"t.name LIKE '%abc%'");

	[Fact]
	public void NOT_LIKE() =>
		_assert.Equal(
			_t.name.NOT_LIKE(L("%abc%")),
			"t.name NOT LIKE '%abc%'");
}
