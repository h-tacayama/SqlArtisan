using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void ABS_NumericValue_CorrectSql()
	{
		SqlStatement sql =
			SELECT(ABS(_t.code))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("ABS(\"t\".code)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void ADD_MONTHS_DateTimeAndNumeric_CorrectSql()
	{
		SqlStatement sql =
			SELECT(ADD_MONTHS(_t.created_at, L(3)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("ADD_MONTHS(\"t\".created_at, 3)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void AVG_NumericValue_CorrectSql()
	{
		SqlStatement sql =
			SELECT(AVG(_t.code))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("AVG(\"t\".code)");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void AVG_DISTINCT_NumericValue_CorrectSql()
	{
		SqlStatement sql =
			SELECT(AVG(DISTINCT, _t.code))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("AVG(DISTINCT \"t\".code)");

		Assert.Equal(expected.ToString(), sql.Text);
	}
}
