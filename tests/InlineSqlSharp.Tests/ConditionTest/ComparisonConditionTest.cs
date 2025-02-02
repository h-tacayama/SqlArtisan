using static InlineSqlSharp.Oracle.SqlWordbook;

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
	public void Character_Expr_Equality() =>
		_assert.Equal(_t.name == L("abc"), "t.name = 'abc'");

	[Fact]
	public void Character_Expr_Inequality() =>
		_assert.Equal(_t.name != L("abc"), "t.name <> 'abc'");

	[Fact]
	public void Character_Expr_LessThan() =>
		_assert.Equal(_t.name < L("abc"), "t.name < 'abc'");

	[Fact]
	public void Character_Expr_GreaterThan() =>
		_assert.Equal(_t.name > L("abc"), "t.name > 'abc'");

	[Fact]
	public void Character_Expr_LessThanOrEqual() =>
		_assert.Equal(_t.name <= L("abc"), "t.name <= 'abc'");

	[Fact]
	public void Character_Expr_GreaterThanOrEqual() =>
		_assert.Equal(_t.name >= L("abc"), "t.name >= 'abc'");

	[Fact]
	public void Character_Bind_Equality() =>
		_assert.Equal(_t.name == "abc", "t.name = :B_0", 1);

	[Fact]
	public void Character_Bind_Inequality() =>
		_assert.Equal(_t.name != "abc", "t.name <> :B_0", 1);

	[Fact]
	public void Character_Bind_LessThan() =>
		_assert.Equal(_t.name < "abc", "t.name < :B_0", 1);

	[Fact]
	public void Character_Bind_GreaterThan() =>
		_assert.Equal(_t.name > "abc", "t.name > :B_0", 1);

	[Fact]
	public void Character_Bind_LessThanOrEqual() =>
		_assert.Equal(_t.name <= "abc", "t.name <= :B_0", 1);

	[Fact]
	public void Character_Bind_GreaterThanOrEqual() =>
		_assert.Equal(_t.name >= "abc", "t.name >= :B_0", 1);

	[Fact]
	public void DateTime_Expr_Equality() =>
		_assert.Equal(_t.created_at == _t.created_at, "t.created_at = t.created_at");

	[Fact]
	public void DateTime_Expr_Inequality() =>
		_assert.Equal(_t.created_at != _t.created_at, "t.created_at <> t.created_at");

	[Fact]
	public void DateTime_Expr_LessThan() =>
		_assert.Equal(_t.created_at < _t.created_at, "t.created_at < t.created_at");

	[Fact]
	public void DateTime_Expr_GreaterThan() =>
		_assert.Equal(_t.created_at > _t.created_at, "t.created_at > t.created_at");

	[Fact]
	public void DateTime_Expr_LessThanOrEqual() =>
		_assert.Equal(_t.created_at <= _t.created_at, "t.created_at <= t.created_at");

	[Fact]
	public void DateTime_Expr_GreaterThanOrEqual() =>
		_assert.Equal(_t.created_at >= _t.created_at, "t.created_at >= t.created_at");

	[Fact]
	public void DateTime_Bind_Equality() =>
		_assert.Equal(_t.created_at == new DateTime(2001, 2, 3), "t.created_at = :B_0", 1);

	[Fact]
	public void DateTime_Bind_Inequality() =>
		_assert.Equal(_t.created_at != new DateTime(2001, 2, 3), "t.created_at <> :B_0", 1);

	[Fact]
	public void DateTime_Bind_LessThan() =>
		_assert.Equal(_t.created_at < new DateTime(2001, 2, 3), "t.created_at < :B_0", 1);

	[Fact]
	public void DateTime_Bind_GreaterThan() =>
		_assert.Equal(_t.created_at > new DateTime(2001, 2, 3), "t.created_at > :B_0", 1);

	[Fact]
	public void DateTime_Bind_LessThanOrEqual() =>
		_assert.Equal(_t.created_at <= new DateTime(2001, 2, 3), "t.created_at <= :B_0", 1);

	[Fact]
	public void DateTime_Bind_GreaterThanOrEqual() =>
		_assert.Equal(_t.created_at >= new DateTime(2001, 2, 3), "t.created_at >= :B_0", 1);

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
		_assert.Equal(_t.code == 1, "t.code = :B_0", 1);

	[Fact]
	public void Int_Bind_Inequality() =>
		_assert.Equal(_t.code != 1, "t.code <> :B_0", 1);

	[Fact]
	public void Int_Bind_LessThan() =>
		_assert.Equal(_t.code < 1, "t.code < :B_0", 1);

	[Fact]
	public void Int_Bind_GreaterThan() =>
		_assert.Equal(_t.code > 1, "t.code > :B_0", 1);

	[Fact]
	public void Int_Bind_LessThanOrEqual() =>
		_assert.Equal(_t.code <= 1, "t.code <= :B_0", 1);

	[Fact]
	public void Int_Bind_GreaterThanOrEqual() =>
		_assert.Equal(_t.code >= 1, "t.code >= :B_0", 1);

	[Fact]
	public void Long_Bind_Equality() =>
		_assert.Equal(_t.code == (long)1, "t.code = :B_0", 1);

	[Fact]
	public void Long_Bind_Inequality() =>
		_assert.Equal(_t.code != (long)1, "t.code <> :B_0", 1);

	[Fact]
	public void Long_Bind_LessThan() =>
		_assert.Equal(_t.code < (long)1, "t.code < :B_0", 1);

	[Fact]
	public void Long_Bind_GreaterThan() =>
		_assert.Equal(_t.code > (long)1, "t.code > :B_0", 1);

	[Fact]
	public void Long_Bind_LessThanOrEqual() =>
		_assert.Equal(_t.code <= (long)1, "t.code <= :B_0", 1);

	[Fact]
	public void Long_Bind_GreaterThanOrEqual() =>
		_assert.Equal(_t.code >= (long)1, "t.code >= :B_0", 1);

	[Fact]
	public void Float_Bind_Equality() =>
		_assert.Equal(_t.code == (float)1, "t.code = :B_0", 1);

	[Fact]
	public void Float_Bind_Inequality() =>
		_assert.Equal(_t.code != (float)1, "t.code <> :B_0", 1);

	[Fact]
	public void Float_Bind_LessThan() =>
		_assert.Equal(_t.code < (float)1, "t.code < :B_0", 1);

	[Fact]
	public void Float_Bind_GreaterThan() =>
		_assert.Equal(_t.code > (float)1, "t.code > :B_0", 1);

	[Fact]
	public void Float_Bind_LessThanOrEqual() =>
		_assert.Equal(_t.code <= (float)1, "t.code <= :B_0", 1);

	[Fact]
	public void Float_Bind_GreaterThanOrEqual() =>
		_assert.Equal(_t.code >= (float)1, "t.code >= :B_0", 1);

	[Fact]
	public void Double_Bind_Equality() =>
		_assert.Equal(_t.code == (double)1, "t.code = :B_0", 1);

	[Fact]
	public void Double_Bind_Inequality() =>
		_assert.Equal(_t.code != (double)1, "t.code <> :B_0", 1);

	[Fact]
	public void Double_Bind_LessThan() =>
		_assert.Equal(_t.code < (double)1, "t.code < :B_0", 1);

	[Fact]
	public void Double_Bind_GreaterThan() =>
		_assert.Equal(_t.code > (double)1, "t.code > :B_0", 1);

	[Fact]
	public void Double_Bind_LessThanOrEqual() =>
		_assert.Equal(_t.code <= (double)1, "t.code <= :B_0", 1);

	[Fact]
	public void Double_Bind_GreaterThanOrEqual() =>
		_assert.Equal(_t.code >= (double)1, "t.code >= :B_0", 1);

	[Fact]
	public void Decimal_Bind_Equality() =>
		_assert.Equal(_t.code == (decimal)1, "t.code = :B_0", 1);

	[Fact]
	public void Decimal_Bind_Inequality() =>
		_assert.Equal(_t.code != (decimal)1, "t.code <> :B_0", 1);

	[Fact]
	public void Decimal_Bind_LessThan() =>
		_assert.Equal(_t.code < (decimal)1, "t.code < :B_0", 1);

	[Fact]
	public void Decimal_Bind_GreaterThan() =>
		_assert.Equal(_t.code > (decimal)1, "t.code > :B_0", 1);

	[Fact]
	public void Decimal_Bind_LessThanOrEqual() =>
		_assert.Equal(_t.code <= (decimal)1, "t.code <= :B_0", 1);

	[Fact]
	public void Decimal_Bind_GreaterThanOrEqual() =>
		_assert.Equal(_t.code >= (decimal)1, "t.code >= :B_0", 1);
}
