using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class NumericExpr_InTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public NumericExpr_InTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void NumericExpr_IN_SingleLiteral_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append("1");
		expected.Append(")");

		_assert.Equal(_t.code.IN(L(1)), expected.ToString());
	}

	[Fact]
	public void NumericExpr_IN_MultipleSBytes_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		sbyte[] values = [1, 2, 3];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, (sbyte)1, (sbyte)2, (sbyte)3);
	}

	[Fact]
	public void NumericExpr_IN_MultipleBytes_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		byte[] values = [1, 2, 3];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, (byte)1, (byte)2, (byte)3);
	}

	[Fact]
	public void NumericExpr_IN_MultipleShorts_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		short[] values = [1, 2, 3];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, (short)1, (short)2, (short)3);
	}

	[Fact]
	public void NumericExpr_IN_MultipleUShorts_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		ushort[] values = [1, 2, 3];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, (ushort)1, (ushort)2, (ushort)3);
	}

	[Fact]
	public void NumericExpr_IN_MultipleInts_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		int[] values = [1, 2, 3];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, 1, 2, 3);
	}

	[Fact]
	public void NumericExpr_IN_MultipleUInts_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		uint[] values = [1, 2, 3];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, (uint)1, (uint)2, (uint)3);
	}

	[Fact]
	public void NumericExpr_IN_MultipleLongs_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		long[] values = [1, 2, 3];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, (long)1, (long)2, (long)3);
	}

	[Fact]
	public void NumericExpr_IN_MultipleULongs_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		ulong[] values = [1, 2, 3];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, (ulong)1, (ulong)2, (ulong)3);
	}

	[Fact]
	public void NumericExpr_IN_MultipleFloats_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		float[] values = [1.0f, 2.0f, 3.0f];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, 1.0f, 2.0f, 3.0f);
	}

	[Fact]
	public void NumericExpr_IN_MultipleDoubles_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		double[] values = [1.0, 2.0, 3.0];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, 1.0, 2.0, 3.0);
	}

	[Fact]
	public void NumericExpr_IN_MultipleDecimals_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		decimal[] values = [1.0m, 2.0m, 3.0m];
		_assert.Equal(_t.code.IN(values),
			expected.ToString(),
			3, 1.0m, 2.0m, 3.0m);
	}

	[Fact]
	public void NumericExpr_NOT_IN_SingleLiteral_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append("1");
		expected.Append(")");

		_assert.Equal(_t.code.NOT_IN(L(1)), expected.ToString());
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleSBytes_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		sbyte[] values = [1, 2, 3];
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, (sbyte)1, (sbyte)2, (sbyte)3);
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleBytes_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		byte[] values = { 1, 2, 3 };
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, (byte)1, (byte)2, (byte)3);
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleShorts_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		short[] values = [1, 2, 3];
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, (short)1, (short)2, (short)3);
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleUShorts_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		ushort[] values = [1, 2, 3];
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, (ushort)1, (ushort)2, (ushort)3);
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleInts_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		int[] values = [1, 2, 3];
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, 1, 2, 3);
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleUInts_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		uint[] values = [1, 2, 3];
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, (uint)1, (uint)2, (uint)3);
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleLongs_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		long[] values = [1, 2, 3];
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, (long)1, (long)2, (long)3);
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleULongs_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		ulong[] values = [1, 2, 3];
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, (ulong)1, (ulong)2, (ulong)3);
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleFloats_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		float[] values = [1.0f, 2.0f, 3.0f];
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, 1.0f, 2.0f, 3.0f);
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleDoubles_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		double[] values = [1.0, 2.0, 3.0];
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, 1.0, 2.0, 3.0);
	}

	[Fact]
	public void NumericExpr_NOT_IN_MultipleDecimals_CorrectSql()
	{
		StringBuilder expected = new();
		expected.Append("\"t\".code NOT IN ");
		expected.Append("(");
		expected.Append(":0, ");
		expected.Append(":1, ");
		expected.Append(":2");
		expected.Append(")");

		decimal[] values = [1.0m, 2.0m, 3.0m];
		_assert.Equal(_t.code.NOT_IN(values),
			expected.ToString(),
			3, 1.0m, 2.0m, 3.0m);
	}
}
