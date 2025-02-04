using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class InConditionTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public InConditionTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void Character_IN_Single() =>
		_assert.Equal(_t.name.IN(L("a")), "t.name IN ('a')");

	[Fact]
	public void Character_IN_Multi() =>
		_assert.Equal(_t.name.IN(P("a"), P("b"), P("c")),
			"t.name IN (:P_0, :P_1, :P_2)", 3);

	[Fact]
	public void Character_NOT_IN_Single() =>
		_assert.Equal(_t.name.NOT_IN(L("a")), "t.name NOT IN ('a')");

	[Fact]
	public void DateTime_IN_Single() =>
		_assert.Equal(_t.created_at.IN(P(new DateTime(2001, 2, 3))),
			"t.created_at IN (:P_0)", 1);

	[Fact]
	public void DateTime_IN_Multi() =>
		_assert.Equal(_t.created_at.IN(
			P(new DateTime(2001, 2, 3)),
			P(new DateTime(2001, 2, 4)),
			P(new DateTime(2001, 2, 5))),
			"t.created_at IN (:P_0, :P_1, :P_2)", 3);

	[Fact]
	public void DateTime_NOT_IN_Single() =>
		_assert.Equal(_t.created_at.NOT_IN(P(new DateTime(2001, 2, 3))),
			"t.created_at NOT IN (:P_0)", 1);

	[Fact]
	public void Numeric_IN_Single() =>
		_assert.Equal(_t.code.IN(L(1)), "t.code IN (1)");

	[Fact]
	public void Numeric_IN_Multi() =>
		_assert.Equal(_t.code.IN(P(1), P(2), P(3)),
			"t.code IN (:P_0, :P_1, :P_2)", 3);

	[Fact]
	public void Numeric_NOT_IN_Single() =>
		_assert.Equal(_t.code.NOT_IN(L(1)), "t.code NOT IN (1)");
}
