using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void SELECT_UPPER()
	{
		SqlCommand sql =
			SELECT(UPPER(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("UPPER(t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
