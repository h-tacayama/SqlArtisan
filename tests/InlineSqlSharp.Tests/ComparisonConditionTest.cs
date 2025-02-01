using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class ComparisonConditionTest
{
	private test_table _t = new("t");

	[Fact]
	public void Character_Expr_Equality() =>
		TestImpl(_t.name == L("abc"), "t.name = 'abc'", 0);

	[Fact]
	public void Character_Expr_Inequality() =>
		TestImpl(_t.name != L("abc"), "t.name <> 'abc'", 0);

	[Fact]
	public void Character_Expr_LessThan() =>
		TestImpl(_t.name < L("abc"), "t.name < 'abc'", 0);

	[Fact]
	public void Character_Expr_GreaterThan() =>
		TestImpl(_t.name > L("abc"), "t.name > 'abc'", 0);

	[Fact]
	public void Character_Expr_LessThanOrEqual() =>
		TestImpl(_t.name <= L("abc"), "t.name <= 'abc'", 0);

	[Fact]
	public void Character_Expr_GreaterThanOrEqual() =>
		TestImpl(_t.name >= L("abc"), "t.name >= 'abc'", 0);

	[Fact]
	public void Character_Bind_Equality() =>
		TestImpl(_t.name == "abc", "t.name = :B_0", 1);

	[Fact]
	public void Character_Bind_Inequality() =>
		TestImpl(_t.name != "abc", "t.name <> :B_0", 1);

	[Fact]
	public void Character_Bind_LessThan() =>
		TestImpl(_t.name < "abc", "t.name < :B_0", 1);

	[Fact]
	public void Character_Bind_GreaterThan() =>
		TestImpl(_t.name > "abc", "t.name > :B_0", 1);

	[Fact]
	public void Character_Bind_LessThanOrEqual() =>
		TestImpl(_t.name <= "abc", "t.name <= :B_0", 1);

	[Fact]
	public void Character_Bind_GreaterThanOrEqual() =>
		TestImpl(_t.name >= "abc", "t.name >= :B_0", 1);

	[Fact]
	public void DateTime_Expr_Equality() =>
		TestImpl(_t.created_at == _t.created_at, "t.created_at = t.created_at", 0);

	[Fact]
	public void DateTime_Expr_Inequality() =>
		TestImpl(_t.created_at != _t.created_at, "t.created_at <> t.created_at", 0);

	[Fact]
	public void DateTime_Expr_LessThan() =>
		TestImpl(_t.created_at < _t.created_at, "t.created_at < t.created_at", 0);

	[Fact]
	public void DateTime_Expr_GreaterThan() =>
		TestImpl(_t.created_at > _t.created_at, "t.created_at > t.created_at", 0);

	[Fact]
	public void DateTime_Expr_LessThanOrEqual() =>
		TestImpl(_t.created_at <= _t.created_at, "t.created_at <= t.created_at", 0);

	[Fact]
	public void DateTime_Expr_GreaterThanOrEqual() =>
		TestImpl(_t.created_at >= _t.created_at, "t.created_at >= t.created_at", 0);

	[Fact]
	public void DateTime_Bind_Equality() =>
		TestImpl(_t.created_at == new DateTime(2001, 2, 3), "t.created_at = :B_0", 1);

	[Fact]
	public void DateTime_Bind_Inequality() =>
		TestImpl(_t.created_at != new DateTime(2001, 2, 3), "t.created_at <> :B_0", 1);

	[Fact]
	public void DateTime_Bind_LessThan() =>
		TestImpl(_t.created_at < new DateTime(2001, 2, 3), "t.created_at < :B_0", 1);

	[Fact]
	public void DateTime_Bind_GreaterThan() =>
		TestImpl(_t.created_at > new DateTime(2001, 2, 3), "t.created_at > :B_0", 1);

	[Fact]
	public void DateTime_Bind_LessThanOrEqual() =>
		TestImpl(_t.created_at <= new DateTime(2001, 2, 3), "t.created_at <= :B_0", 1);

	[Fact]
	public void DateTime_Bind_GreaterThanOrEqual() =>
		TestImpl(_t.created_at >= new DateTime(2001, 2, 3), "t.created_at >= :B_0", 1);

	[Fact]
	public void Numeric_Expr_Equality() =>
		TestImpl(_t.code == L(1), "t.code = 1", 0);

	[Fact]
	public void Numeric_Expr_Inequality() =>
		TestImpl(_t.code != L(1), "t.code <> 1", 0);

	[Fact]
	public void Numeric_Expr_LessThan() =>
		TestImpl(_t.code < L(1), "t.code < 1", 0);

	[Fact]
	public void Numeric_Expr_GreaterThan() =>
		TestImpl(_t.code > L(1), "t.code > 1", 0);

	[Fact]
	public void Numeric_Expr_LessThanOrEqual() =>
		TestImpl(_t.code <= L(1), "t.code <= 1", 0);

	[Fact]
	public void Numeric_Expr_GreaterThanOrEqual() =>
		TestImpl(_t.code >= L(1), "t.code >= 1", 0);

	[Fact]
	public void Int_Bind_Equality() =>
		TestImpl(_t.code == 1, "t.code = :B_0", 1);

	[Fact]
	public void Int_Bind_Inequality() =>
		TestImpl(_t.code != 1, "t.code <> :B_0", 1);

	[Fact]
	public void Int_Bind_LessThan() =>
		TestImpl(_t.code < 1, "t.code < :B_0", 1);

	[Fact]
	public void Int_Bind_GreaterThan() =>
		TestImpl(_t.code > 1, "t.code > :B_0", 1);

	[Fact]
	public void Int_Bind_LessThanOrEqual() =>
		TestImpl(_t.code <= 1, "t.code <= :B_0", 1);

	[Fact]
	public void Int_Bind_GreaterThanOrEqual() =>
		TestImpl(_t.code >= 1, "t.code >= :B_0", 1);

	[Fact]
	public void Long_Bind_Equality() =>
		TestImpl(_t.code == (long)1, "t.code = :B_0", 1);

	[Fact]
	public void Long_Bind_Inequality() =>
		TestImpl(_t.code != (long)1, "t.code <> :B_0", 1);

	[Fact]
	public void Long_Bind_LessThan() =>
		TestImpl(_t.code < (long)1, "t.code < :B_0", 1);

	[Fact]
	public void Long_Bind_GreaterThan() =>
		TestImpl(_t.code > (long)1, "t.code > :B_0", 1);

	[Fact]
	public void Long_Bind_LessThanOrEqual() =>
		TestImpl(_t.code <= (long)1, "t.code <= :B_0", 1);

	[Fact]
	public void Long_Bind_GreaterThanOrEqual() =>
		TestImpl(_t.code >= (long)1, "t.code >= :B_0", 1);

	[Fact]
	public void Float_Bind_Equality() =>
		TestImpl(_t.code == (float)1, "t.code = :B_0", 1);

	[Fact]
	public void Float_Bind_Inequality() =>
		TestImpl(_t.code != (float)1, "t.code <> :B_0", 1);

	[Fact]
	public void Float_Bind_LessThan() =>
		TestImpl(_t.code < (float)1, "t.code < :B_0", 1);

	[Fact]
	public void Float_Bind_GreaterThan() =>
		TestImpl(_t.code > (float)1, "t.code > :B_0", 1);

	[Fact]
	public void Float_Bind_LessThanOrEqual() =>
		TestImpl(_t.code <= (float)1, "t.code <= :B_0", 1);

	[Fact]
	public void Float_Bind_GreaterThanOrEqual() =>
		TestImpl(_t.code >= (float)1, "t.code >= :B_0", 1);

	[Fact]
	public void Double_Bind_Equality() =>
		TestImpl(_t.code == (double)1, "t.code = :B_0", 1);

	[Fact]
	public void Double_Bind_Inequality() =>
		TestImpl(_t.code != (double)1, "t.code <> :B_0", 1);

	[Fact]
	public void Double_Bind_LessThan() =>
		TestImpl(_t.code < (double)1, "t.code < :B_0", 1);

	[Fact]
	public void Double_Bind_GreaterThan() =>
		TestImpl(_t.code > (double)1, "t.code > :B_0", 1);

	[Fact]
	public void Double_Bind_LessThanOrEqual() =>
		TestImpl(_t.code <= (double)1, "t.code <= :B_0", 1);

	[Fact]
	public void Double_Bind_GreaterThanOrEqual() =>
		TestImpl(_t.code >= (double)1, "t.code >= :B_0", 1);

	[Fact]
	public void Decimal_Bind_Equality() =>
		TestImpl(_t.code == (decimal)1, "t.code = :B_0", 1);

	[Fact]
	public void Decimal_Bind_Inequality() =>
		TestImpl(_t.code != (decimal)1, "t.code <> :B_0", 1);

	[Fact]
	public void Decimal_Bind_LessThan() =>
		TestImpl(_t.code < (decimal)1, "t.code < :B_0", 1);

	[Fact]
	public void Decimal_Bind_GreaterThan() =>
		TestImpl(_t.code > (decimal)1, "t.code > :B_0", 1);

	[Fact]
	public void Decimal_Bind_LessThanOrEqual() =>
		TestImpl(_t.code <= (decimal)1, "t.code <= :B_0", 1);

	[Fact]
	public void Decimal_Bind_GreaterThanOrEqual() =>
		TestImpl(_t.code >= (decimal)1, "t.code >= :B_0", 1);

	private void TestImpl(
		ICondition testCondition,
		string expectedSql,
		int expectedBindCount)
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.WHERE(testCondition)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("WHERE");
		expected.Append(expectedSql);

		Assert.Equal(expected.ToString(), sql.Statement);
		Assert.Equal(expectedBindCount, sql.Parameters.Count);
	}
}
