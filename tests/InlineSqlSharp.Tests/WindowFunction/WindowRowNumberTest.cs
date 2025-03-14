using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class WindowRowNumberTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void SELECT_ROW_NUMBER_OVER()
	{
		SqlCommand sql =
			SELECT(ROW_NUMBER().OVER())
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("ROW_NUMBER()");
		expected.Append("OVER()");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_ROW_NUMBER_OVER_ORDER_BY()
	{
		SqlCommand sql =
			SELECT(ROW_NUMBER().OVER(ORDER_BY(_t.code, _t.name)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("ROW_NUMBER()");
		expected.AppendLine("OVER");
		expected.AppendLine("(");
		expected.AppendLine("ORDER BY");
		expected.AppendLine("t.code");
		expected.AppendLine(", t.name");
		expected.Append(")");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_ROW_NUMBER_OVER_PARTITION_BY_ORDER_BY()
	{
		SqlCommand sql =
			SELECT(ROW_NUMBER().OVER(
				PARTITION_BY(_t.code, _t.name)
				.ORDER_BY(_t.code.ASC, _t.name.DESC)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("ROW_NUMBER()");
		expected.AppendLine("OVER");
		expected.AppendLine("(");
		expected.AppendLine("PARTITION BY");
		expected.AppendLine("t.code");
		expected.AppendLine(", t.name");
		expected.AppendLine("ORDER BY");
		expected.AppendLine("t.code ASC");
		expected.AppendLine(", t.name DESC");
		expected.Append(")");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
