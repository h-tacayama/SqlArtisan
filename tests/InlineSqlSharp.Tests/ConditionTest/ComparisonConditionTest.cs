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
	public void Character_Bind_Equality() =>
		_assert.Equal(_t.name == P("abc"), "t.name = :P_0", 1);

	[Fact]
	public void Character_Bind_Inequality() =>
		_assert.Equal(_t.name != P("abc"), "t.name <> :P_0", 1);

	[Fact]
	public void Character_Bind_LessThan() =>
		_assert.Equal(_t.name < P("abc"), "t.name < :P_0", 1);

	[Fact]
	public void Character_Bind_GreaterThan() =>
		_assert.Equal(_t.name > P("abc"), "t.name > :P_0", 1);

	[Fact]
	public void Character_Bind_LessThanOrEqual() =>
		_assert.Equal(_t.name <= P("abc"), "t.name <= :P_0", 1);

	[Fact]
	public void Character_Bind_GreaterThanOrEqual() =>
		_assert.Equal(_t.name >= P("abc"), "t.name >= :P_0", 1);

	[Fact]
	public void DateTime_Bind_Equality() =>
		_assert.Equal(_t.created_at == P(new DateTime(2001, 2, 3)), "t.created_at = :P_0", 1);

	[Fact]
	public void DateTime_Bind_Inequality() =>
		_assert.Equal(_t.created_at != P(new DateTime(2001, 2, 3)), "t.created_at <> :P_0", 1);

	[Fact]
	public void DateTime_Bind_LessThan() =>
		_assert.Equal(_t.created_at < P(new DateTime(2001, 2, 3)), "t.created_at < :P_0", 1);

	[Fact]
	public void DateTime_Bind_GreaterThan() =>
		_assert.Equal(_t.created_at > P(new DateTime(2001, 2, 3)), "t.created_at > :P_0", 1);

	[Fact]
	public void DateTime_Bind_LessThanOrEqual() =>
		_assert.Equal(_t.created_at <= P(new DateTime(2001, 2, 3)), "t.created_at <= :P_0", 1);

	[Fact]
	public void DateTime_Bind_GreaterThanOrEqual() =>
		_assert.Equal(_t.created_at >= P(new DateTime(2001, 2, 3)), "t.created_at >= :P_0", 1);

	[Fact]
	public void Numeric_Expr_Equality() =>
		_assert.Equal(_t.code == L(1), "t.code = 1");

	[Fact]
	public void Numeric_Expr_Inequality() =>
		_assert.Equal(_t.code != L(1), "t.code <> 1");

	[Fact]
	public void Numeric_Expr_LessThan() =>
		_assert.Equal(_t.code < L(1), "t.code < 1");

	[Fact]
	public void Numeric_Expr_GreaterThan() =>
		_assert.Equal(_t.code > L(1), "t.code > 1");

	[Fact]
	public void Numeric_Expr_LessThanOrEqual() =>
		_assert.Equal(_t.code <= L(1), "t.code <= 1");

	[Fact]
	public void Numeric_Expr_GreaterThanOrEqual() =>
		_assert.Equal(_t.code >= L(1), "t.code >= 1");

	[Fact]
	public void Int_Bind_Equality() =>
		_assert.Equal(_t.code == P(1), "t.code = :P_0", 1);

	[Fact]
	public void Int_Bind_Inequality() =>
		_assert.Equal(_t.code != P(1), "t.code <> :P_0", 1);

	[Fact]
	public void Int_Bind_LessThan() =>
		_assert.Equal(_t.code < P(1), "t.code < :P_0", 1);

	[Fact]
	public void Int_Bind_GreaterThan() =>
		_assert.Equal(_t.code > P(1), "t.code > :P_0", 1);

	[Fact]
	public void Int_Bind_LessThanOrEqual() =>
		_assert.Equal(_t.code <= P(1), "t.code <= :P_0", 1);

	[Fact]
	public void Int_Bind_GreaterThanOrEqual() =>
		_assert.Equal(_t.code >= P(1), "t.code >= :P_0", 1);
}
