using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class UpdateTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void UPDATE_SET()
	{
		SqlCommand sql =
			UPDATE(_t)
			.SET(
				_t.code == L(1),
				_t.name == L("a"),
				_t.created_at == SYSDATE)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("UPDATE");
		expected.AppendLine("test_table t");
		expected.AppendLine("SET");
		expected.AppendLine("t.code = 1");
		expected.AppendLine(", t.name = 'a'");
		expected.Append(", t.created_at = SYSDATE");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void UPDATE_SET_Inequality_then_Throw_ArgumentException()
	{
		Assert.Throws<ArgumentException>(() =>
		{
			UPDATE(_t)
			.SET(
				_t.code != L(1))
			.Build();
		});
	}
}
