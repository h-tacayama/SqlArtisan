using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void SELECT_AVG()
	{
		SqlCommand sql =
			SELECT(AVG(_t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("AVG(t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_AVG_DISTINCT()
	{
		SqlCommand sql =
			SELECT(AVG(DISTINCT, _t.code))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("AVG(DISTINCT t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
