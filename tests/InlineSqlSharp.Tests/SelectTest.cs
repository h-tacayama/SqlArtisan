using System.Text;
using static InlineSqlSharp.SqlWordbook;

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

	[Fact]
	public void SELECT_Parameters()
	{
		SqlCommand sql =
			SELECT(
				P("abc"),
				P(new DateTime(2001, 2, 3)),
				P((int)1),
				P((long)2),
				P((float)3.3),
				P((double)4.4),
				P((decimal)5.5))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine(":P_0");
		expected.AppendLine(", :P_1");
		expected.AppendLine(", :P_2");
		expected.AppendLine(", :P_3");
		expected.AppendLine(", :P_4");
		expected.AppendLine(", :P_5");
		expected.Append(", :P_6");

		Assert.Equal(expected.ToString(), sql.Statement);
		Assert.Equal("abc", sql.Parameters[0].Value);
		Assert.Equal(new DateTime(2001, 2, 3), sql.Parameters[1].Value);
		Assert.Equal((int)1, sql.Parameters[2].Value);
		Assert.Equal((long)2, sql.Parameters[3].Value);
		Assert.Equal((float)3.3, sql.Parameters[4].Value);
		Assert.Equal((double)4.4, sql.Parameters[5].Value);
		Assert.Equal((decimal)5.5, sql.Parameters[6].Value);
	}
}
