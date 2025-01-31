using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class WhereTest
{
	private test_table _t = new("t");

	[Fact]
	public void WHERE_Char_Expr_Equality() =>
		WHERE_Impl(_t.name == L("abc"), "t.name = 'abc'", 0);

	[Fact]
	public void WHERE_Char_Expr_Inequality() =>
		WHERE_Impl(_t.name != L("abc"), "t.name <> 'abc'", 0);

	[Fact]
	public void WHERE_Char_Expr_LessThan() =>
		WHERE_Impl(_t.name < L("abc"), "t.name < 'abc'", 0);

	[Fact]
	public void WHERE_Char_Expr_GreaterThan() =>
		WHERE_Impl(_t.name > L("abc"), "t.name > 'abc'", 0);

	[Fact]
	public void WHERE_Char_Expr_LessThanOrEqual() =>
		WHERE_Impl(_t.name <= L("abc"), "t.name <= 'abc'", 0);

	[Fact]
	public void WHERE_Char_Expr_GreaterThanOrEqual() =>
		WHERE_Impl(_t.name >= L("abc"), "t.name >= 'abc'", 0);

	[Fact]
	public void WHERE_Char_Bind_Equality() =>
		WHERE_Impl(_t.name == "abc", "t.name = :B_0", 1);

	[Fact]
	public void WHERE_Char_Bind_Inequality() =>
		WHERE_Impl(_t.name != "abc", "t.name <> :B_0", 1);

	[Fact]
	public void WHERE_Char_Bind_LessThan() =>
		WHERE_Impl(_t.name < "abc", "t.name < :B_0", 1);

	[Fact]
	public void WHERE_Char_Bind_GreaterThan() =>
		WHERE_Impl(_t.name > "abc", "t.name > :B_0", 1);

	[Fact]
	public void WHERE_Char_Bind_LessThanOrEqual() =>
		WHERE_Impl(_t.name <= "abc", "t.name <= :B_0", 1);

	[Fact]
	public void WHERE_Char_Bind_GreaterThanOrEqual() =>
		WHERE_Impl(_t.name >= "abc", "t.name >= :B_0", 1);

	[Fact]
	public void WHERE_Date_Expr_Equality() =>
		WHERE_Impl(_t.created_at == _t.created_at, "t.created_at = t.created_at", 0);

	[Fact]
	public void WHERE_Date_Expr_Inequality() =>
		WHERE_Impl(_t.created_at != _t.created_at, "t.created_at <> t.created_at", 0);

	[Fact]
	public void WHERE_Date_Expr_LessThan() =>
		WHERE_Impl(_t.created_at < _t.created_at, "t.created_at < t.created_at", 0);

	[Fact]
	public void WHERE_Date_Expr_GreaterThan() =>
		WHERE_Impl(_t.created_at > _t.created_at, "t.created_at > t.created_at", 0);

	[Fact]
	public void WHERE_Date_Expr_LessThanOrEqual() =>
		WHERE_Impl(_t.created_at <= _t.created_at, "t.created_at <= t.created_at", 0);

	[Fact]
	public void WHERE_Date_Expr_GreaterThanOrEqual() =>
		WHERE_Impl(_t.created_at >= _t.created_at, "t.created_at >= t.created_at", 0);

	[Fact]
	public void WHERE_Num_Expr_Equality() =>
		WHERE_Impl(_t.code == L(1), "t.code = 1", 0);

	[Fact]
	public void WHERE_Num_Expr_Inequality() =>
		WHERE_Impl(_t.code != L(1), "t.code <> 1", 0);

	[Fact]
	public void WHERE_Date_Bind_Equality() =>
		WHERE_Impl(_t.created_at == new DateTime(2001, 2, 3), "t.created_at = :B_0", 1);

	[Fact]
	public void WHERE_Date_Bind_Inequality() =>
		WHERE_Impl(_t.created_at != new DateTime(2001, 2, 3), "t.created_at <> :B_0", 1);

	[Fact]
	public void WHERE_Date_Bind_LessThan() =>
		WHERE_Impl(_t.created_at < new DateTime(2001, 2, 3), "t.created_at < :B_0", 1);

	[Fact]
	public void WHERE_Date_Bind_GreaterThan() =>
		WHERE_Impl(_t.created_at > new DateTime(2001, 2, 3), "t.created_at > :B_0", 1);

	[Fact]
	public void WHERE_Date_Bind_LessThanOrEqual() =>
		WHERE_Impl(_t.created_at <= new DateTime(2001, 2, 3), "t.created_at <= :B_0", 1);

	[Fact]
	public void WHERE_Date_Bind_GreaterThanOrEqual() =>
		WHERE_Impl(_t.created_at >= new DateTime(2001, 2, 3), "t.created_at >= :B_0", 1);

	[Fact]
	public void WHERE_Num_Expr_LessThan() =>
		WHERE_Impl(_t.code < L(1), "t.code < 1", 0);

	[Fact]
	public void WHERE_Num_Expr_GreaterThan() =>
		WHERE_Impl(_t.code > L(1), "t.code > 1", 0);

	[Fact]
	public void WHERE_Num_Expr_LessThanOrEqual() =>
		WHERE_Impl(_t.code <= L(1), "t.code <= 1", 0);

	[Fact]
	public void WHERE_Num_Expr_GreaterThanOrEqual() =>
		WHERE_Impl(_t.code >= L(1), "t.code >= 1", 0);

	[Fact]
	public void WHERE_Int_Bind_Equality() =>
		WHERE_Impl(_t.code == 1, "t.code = :B_0", 1);

	[Fact]
	public void WHERE_Int_Bind_Inequality() =>
		WHERE_Impl(_t.code != 1, "t.code <> :B_0", 1);

	[Fact]
	public void WHERE_Int_Bind_LessThan() =>
		WHERE_Impl(_t.code < 1, "t.code < :B_0", 1);

	[Fact]
	public void WHERE_Int_Bind_GreaterThan() =>
		WHERE_Impl(_t.code > 1, "t.code > :B_0", 1);

	[Fact]
	public void WHERE_Int_Bind_LessThanOrEqual() =>
		WHERE_Impl(_t.code <= 1, "t.code <= :B_0", 1);

	[Fact]
	public void WHERE_Int_Bind_GreaterThanOrEqual() =>
		WHERE_Impl(_t.code >= 1, "t.code >= :B_0", 1);

	[Fact]
	public void WHERE_Long_Bind_Equality() =>
		WHERE_Impl(_t.code == (long)1, "t.code = :B_0", 1);

	[Fact]
	public void WHERE_Long_Bind_Inequality() =>
		WHERE_Impl(_t.code != (long)1, "t.code <> :B_0", 1);

	[Fact]
	public void WHERE_Long_Bind_LessThan() =>
		WHERE_Impl(_t.code < (long)1, "t.code < :B_0", 1);

	[Fact]
	public void WHERE_Long_Bind_GreaterThan() =>
		WHERE_Impl(_t.code > (long)1, "t.code > :B_0", 1);

	[Fact]
	public void WHERE_Long_Bind_LessThanOrEqual() =>
		WHERE_Impl(_t.code <= (long)1, "t.code <= :B_0", 1);

	[Fact]
	public void WHERE_Long_Bind_GreaterThanOrEqual() =>
		WHERE_Impl(_t.code >= (long)1, "t.code >= :B_0", 1);

	[Fact]
	public void WHERE_Float_Bind_Equality() =>
		WHERE_Impl(_t.code == (float)1, "t.code = :B_0", 1);

	[Fact]
	public void WHERE_Float_Bind_Inequality() =>
		WHERE_Impl(_t.code != (float)1, "t.code <> :B_0", 1);

	[Fact]
	public void WHERE_Float_Bind_LessThan() =>
		WHERE_Impl(_t.code < (float)1, "t.code < :B_0", 1);

	[Fact]
	public void WHERE_Float_Bind_GreaterThan() =>
		WHERE_Impl(_t.code > (float)1, "t.code > :B_0", 1);

	[Fact]
	public void WHERE_Float_Bind_LessThanOrEqual() =>
		WHERE_Impl(_t.code <= (float)1, "t.code <= :B_0", 1);

	[Fact]
	public void WHERE_Float_Bind_GreaterThanOrEqual() =>
		WHERE_Impl(_t.code >= (float)1, "t.code >= :B_0", 1);

	[Fact]
	public void WHERE_Double_Bind_Equality() =>
		WHERE_Impl(_t.code == (double)1, "t.code = :B_0", 1);

	[Fact]
	public void WHERE_Double_Bind_Inequality() =>
		WHERE_Impl(_t.code != (double)1, "t.code <> :B_0", 1);

	[Fact]
	public void WHERE_Double_Bind_LessThan() =>
		WHERE_Impl(_t.code < (double)1, "t.code < :B_0", 1);

	[Fact]
	public void WHERE_Double_Bind_GreaterThan() =>
		WHERE_Impl(_t.code > (double)1, "t.code > :B_0", 1);

	[Fact]
	public void WHERE_Double_Bind_LessThanOrEqual() =>
		WHERE_Impl(_t.code <= (double)1, "t.code <= :B_0", 1);

	[Fact]
	public void WHERE_Double_Bind_GreaterThanOrEqual() =>
		WHERE_Impl(_t.code >= (double)1, "t.code >= :B_0", 1);

	[Fact]
	public void WHERE_Decimal_Bind_Equality() =>
		WHERE_Impl(_t.code == (decimal)1, "t.code = :B_0", 1);

	[Fact]
	public void WHERE_Decimal_Bind_Inequality() =>
		WHERE_Impl(_t.code != (decimal)1, "t.code <> :B_0", 1);

	[Fact]
	public void WHERE_Decimal_Bind_LessThan() =>
		WHERE_Impl(_t.code < (decimal)1, "t.code < :B_0", 1);

	[Fact]
	public void WHERE_Decimal_Bind_GreaterThan() =>
		WHERE_Impl(_t.code > (decimal)1, "t.code > :B_0", 1);

	[Fact]
	public void WHERE_Decimal_Bind_LessThanOrEqual() =>
		WHERE_Impl(_t.code <= (decimal)1, "t.code <= :B_0", 1);

	[Fact]
	public void WHERE_Decimal_Bind_GreaterThanOrEqual() =>
		WHERE_Impl(_t.code >= (decimal)1, "t.code >= :B_0", 1);

	private void WHERE_Impl(
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
