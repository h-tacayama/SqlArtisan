using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class SelectTest
{
	private test_table _t = new("t");

	[Fact]
	public void SELECT_FROM()
	{
		SqlCommand sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine(", t.name");
		expected.AppendLine(", t.created_at");
		expected.AppendLine("FROM");
		expected.Append("test_table t");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_Literals()
	{
		SqlCommand sql =
			SELECT(
				L("abc"),
				L((int)1),
				L((long)2),
				L((float)3.3),
				L((double)4.4),
				L((decimal)5.5))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("'abc'");
		expected.AppendLine(", 1");
		expected.AppendLine(", 2");
		expected.AppendLine(", 3.3");
		expected.AppendLine(", 4.4");
		expected.Append(", 5.5");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
