using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void SELECT_NVL_Character()
	{
		SqlCommand sql =
			SELECT(NVL(_t.name, L("Unknown")))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("NVL(t.name, 'Unknown')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_NVL_DateTime()
	{
		SqlCommand sql =
			SELECT(NVL(_t.created_at, TO_DATE(L("2000/01/01"), L("YYYY/MM/DD"))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("NVL(t.created_at, TO_DATE('2000/01/01', 'YYYY/MM/DD'))");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_NVL_Numeric()
	{
		SqlCommand sql =
			SELECT(NVL(_t.code, L(0)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("NVL(t.code, 0)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
