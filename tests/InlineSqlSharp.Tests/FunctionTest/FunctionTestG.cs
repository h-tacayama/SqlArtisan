using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
    [Fact]
	public void SELECT_GREATEST_Character_Column()
	{
		SqlCommand sql =
			SELECT(GREATEST(_t.name, L("test"), _t.name))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("GREATEST(t.name, 'test', t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

    [Fact]
	public void SELECT_GREATEST_DateTime()
	{
		SqlCommand sql =
			SELECT(GREATEST(
				_t.created_at,
				TO_DATE(L("2000/01/01"), L("YYYY/MM/DD")),
				_t.created_at))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("GREATEST(t.created_at, TO_DATE('2000/01/01', 'YYYY/MM/DD'), t.created_at)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

    [Fact]
	public void SELECT_GREATEST_Numeric()
	{
		SqlCommand sql =
			SELECT(GREATEST(_t.code, L(10), _t.code))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("GREATEST(t.code, 10, t.code)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}