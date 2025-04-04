using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class WindowRowNumberTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void ROW_NUMBER_OVER_PartitionByOrderBy_CorrectSql()
	{
		SqlStatement sql =
			SELECT(ROW_NUMBER().OVER(
				PARTITION_BY(_t.code, _t.name)
				.ORDER_BY(_t.code.ASC, _t.name.DESC)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("ROW_NUMBER() ");
		expected.Append("OVER ");
		expected.Append("(");
		expected.Append("PARTITION BY ");
		expected.Append("\"t\".code, ");
		expected.Append("\"t\".name ");
		expected.Append("ORDER BY ");
		expected.Append("\"t\".code ASC, ");
		expected.Append("\"t\".name DESC");
		expected.Append(")");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void ROW_NUMBER_OVER_OrderBy_CorrectSql()
	{
		SqlStatement sql =
			SELECT(ROW_NUMBER().OVER(ORDER_BY(_t.code, _t.name)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("ROW_NUMBER() ");
		expected.Append("OVER ");
		expected.Append("(");
		expected.Append("ORDER BY ");
		expected.Append("\"t\".code, ");
		expected.Append("\"t\".name");
		expected.Append(")");

		Assert.Equal(expected.ToString(), sql.Text);
	}
}
