using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class SelectFromWhereTest
{
	private test_table _t = new("t");

	[Fact]
	public void SELECT_FROM_WHERE_Char_Bind_Equal() =>
		SELECT_FROM_WHERE_Impl(_t.name == "abc", "t.name = :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Char_Bind_NotEqual() =>
		SELECT_FROM_WHERE_Impl(_t.name != "abc", "t.name <> :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Char_Expr_Equal() =>
		SELECT_FROM_WHERE_Impl(_t.name == L("abc"), "t.name = 'abc'", 0);

	[Fact]
	public void SELECT_FROM_WHERE_Char_Expr_NotEqual() =>
		SELECT_FROM_WHERE_Impl(_t.name != L("abc"), "t.name <> 'abc'", 0);

	[Fact]
	public void SELECT_FROM_WHERE_Num_Expr_Equal() =>
		SELECT_FROM_WHERE_Impl(_t.code == L(1), "t.code = 1", 0);

	[Fact]
	public void SELECT_FROM_WHERE_Num_Expr_NotEqual() =>
		SELECT_FROM_WHERE_Impl(_t.code != L(1), "t.code <> 1", 0);

	[Fact]
	public void SELECT_FROM_WHERE_Int_Bind_Equal() =>
		SELECT_FROM_WHERE_Impl(_t.code == 1, "t.code = :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Int_Bind_NotEqual() =>
		SELECT_FROM_WHERE_Impl(_t.code != 1, "t.code <> :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Long_Bind_Equal() =>
		SELECT_FROM_WHERE_Impl(_t.code == (long)1, "t.code = :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Long_Bind_NotEqual() =>
		SELECT_FROM_WHERE_Impl(_t.code != (long)1, "t.code <> :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Float_Bind_Equal() =>
		SELECT_FROM_WHERE_Impl(_t.code == (float)1, "t.code = :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Float_Bind_NotEqual() =>
		SELECT_FROM_WHERE_Impl(_t.code != (float)1, "t.code <> :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Double_Bind_Equal() =>
		SELECT_FROM_WHERE_Impl(_t.code == (double)1, "t.code = :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Double_Bind_NotEqual() =>
		SELECT_FROM_WHERE_Impl(_t.code != (double)1, "t.code <> :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Decimal_Bind_Equal() =>
		SELECT_FROM_WHERE_Impl(_t.code == (decimal)1, "t.code = :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Decimal_Bind_NotEqual() =>
		SELECT_FROM_WHERE_Impl(_t.code != (decimal)1, "t.code <> :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Date_Bind_Equal() =>
		SELECT_FROM_WHERE_Impl(_t.created_at == new DateTime(2001, 2, 3), "t.created_at = :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Date_Bind_NotEqual() =>
		SELECT_FROM_WHERE_Impl(_t.created_at != new DateTime(2001, 2, 3), "t.created_at <> :B_0", 1);

	[Fact]
	public void SELECT_FROM_WHERE_Date_Expr_Equal() =>
		SELECT_FROM_WHERE_Impl(_t.created_at == _t.created_at, "t.created_at = t.created_at", 0);

	[Fact]
	public void SELECT_FROM_WHERE_Date_Expr_NotEqual() =>
		SELECT_FROM_WHERE_Impl(_t.created_at != _t.created_at, "t.created_at <> t.created_at", 0);

	private void SELECT_FROM_WHERE_Impl(
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
