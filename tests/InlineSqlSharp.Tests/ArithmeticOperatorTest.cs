using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class ArithmeticOperatorTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void Addition_NumericValues_CorrectSql() =>
		Assert.Equal("SELECT (1 + 2)", SELECT(L(1) + L(2)).Build().Statement);

	[Fact]
	public void Subtraction_NumericValues_CorrectSql() =>
		Assert.Equal("SELECT (1 - 2)", SELECT(L(1) - L(2)).Build().Statement);

	[Fact]
	public void Multiplication_NumericValues_CorrectSql() =>
		Assert.Equal("SELECT (1 * 2)", SELECT(L(1) * L(2)).Build().Statement);

	[Fact]
	public void Division_NumericValues_CorrectSql() =>
		Assert.Equal("SELECT (1 / 2)", SELECT(L(1) / L(2)).Build().Statement);

	[Fact]
	public void Modulus_NumericValues_CorrectSql() =>
		Assert.Equal("SELECT (1 % 2)", SELECT(L(1) % L(2)).Build().Statement);

	[Fact]
	public void DateOffset_AdditionWithNumber_CorrectSql() =>
		Assert.Equal("SELECT (t.created_at + 1)", SELECT(_t.created_at + L(1)).Build().Statement);

	[Fact]
	public void DateOffset_SubtractionWithNumber_CorrectSql() =>
		Assert.Equal("SELECT (t.created_at - 1)", SELECT(_t.created_at - L(1)).Build().Statement);

	[Fact]
	public void DateDiff_SubtractionAndAddition_CorrectSql() =>
		Assert.Equal("SELECT ((t.created_at - t.created_at) + 1)", SELECT((_t.created_at - _t.created_at) + L(1)).Build().Statement);
}
