using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class SelectTest
{
	[Fact]
	public void SELECT_FROM_Basic()
	{
		test_table t = new("a");
		SqlCommand sql =
			SELECT(
				t.code,
				t.name,
				t.created_at)
			.FROM(t)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("a.code");
		expected.AppendLine(", a.name");
		expected.AppendLine(", a.created_at");
		expected.AppendLine("FROM");
		expected.Append("test_table a");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_Literal()
	{
		SqlCommand sql =
			SELECT(
				L("abc"),
				L((sbyte)1),
				L((byte)2),
				L((short)3),
				L((ushort)4),
				L((int)5),
				L((uint)6),
				L((long)7),
				L((ulong)8),
				L((decimal)9.9),
				L((double)10.10),
				L((decimal)11.11))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("abc");
		expected.AppendLine(", 1");
		expected.AppendLine(", 2");
		expected.AppendLine(", 3");
		expected.AppendLine(", 4");
		expected.AppendLine(", 5");
		expected.AppendLine(", 6");
		expected.AppendLine(", 7");
		expected.AppendLine(", 8");
		expected.AppendLine(", 9.9");
		expected.AppendLine(", 10.1");
		expected.AppendLine(", 11.11");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
