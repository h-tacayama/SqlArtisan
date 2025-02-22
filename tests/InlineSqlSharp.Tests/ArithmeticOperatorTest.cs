using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class ArithmeticOperatorTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void SELECT_Addition()
	{
		SqlCommand sql =
			SELECT(L(1) + L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("(1 + 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_Subtraction()
	{
		SqlCommand sql =
			SELECT(L(1) - L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("(1 - 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_Multiplication()
	{
		SqlCommand sql =
			SELECT(L(1) * L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("(1 * 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_Division()
	{
		SqlCommand sql =
			SELECT(L(1) / L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("(1 / 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_Modulus()
	{
		SqlCommand sql =
			SELECT(L(1) % L(2))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("(1 % 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_DateOffset_Addition()
	{
		SqlCommand sql =
			SELECT(_t.created_at + L(1))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("(t.created_at + 1)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_DateOffset_Subtraction()
	{
		SqlCommand sql =
			SELECT(_t.created_at - L(1))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("(t.created_at - 1)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_DateDiff_Subtraction()
	{
		SqlCommand sql =
			SELECT((_t.created_at - _t.created_at) + L(1))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.Append("((t.created_at - t.created_at) + 1)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
