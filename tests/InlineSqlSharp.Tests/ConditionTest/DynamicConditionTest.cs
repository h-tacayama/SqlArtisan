using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class DynamicConditionTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public DynamicConditionTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void AddConditionIf_Add() =>
		_assert.Equal(AddConditionIf(true, _t.code == L(1)), "t.code = 1");

	[Fact]
	public void AddConditionIf_Not_Add() =>
		_assert.Equal(AddConditionIf(false, _t.code == L(1)), string.Empty);
}
