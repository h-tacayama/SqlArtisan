using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class GroupByTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void GROUP_BY_SingleColumn_CorrectSql()
	{
		SqlStatement sql =
			SELECT(_t.name)
			.FROM(_t)
			.GROUP_BY(_t.name)
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.name ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("GROUP BY ");
		expected.Append("t.name");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void GROUP_BY_MultipleColumns_CorrectSql()
	{
		SqlStatement sql =
			SELECT(
				_t.code,
				_t.name)
			.FROM(_t)
			.GROUP_BY(
				_t.code,
				_t.name)
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("t.code, ");
		expected.Append("t.name ");
		expected.Append("FROM ");
		expected.Append("test_table t ");
		expected.Append("GROUP BY ");
		expected.Append("t.code, ");
		expected.Append("t.name");

		Assert.Equal(expected.ToString(), sql.Text);
	}
}
