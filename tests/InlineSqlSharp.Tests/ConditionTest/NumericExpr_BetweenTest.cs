using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class NumericExpr_BetweenTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	public NumericExpr_BetweenTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void NumericExpr_BETWEEN_Literals_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(L(1), L(10)), "\"t\".code BETWEEN 1 AND 10");

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndSByte_CorrectSql() =>
	_assert.Equal(_t.code.BETWEEN(L(1), (sbyte)10), "\"t\".code BETWEEN 1 AND :0", 1, (sbyte)10);

	[Fact]
	public void NumericExpr_BETWEEN_SByteAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((sbyte)1, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, (sbyte)1);

	[Fact]
	public void NumericExpr_BETWEEN_SByteValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((sbyte)1, (sbyte)10), "\"t\".code BETWEEN :0 AND :1", 2, (sbyte)1, (sbyte)10);

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndByte_CorrectSql() =>
	_assert.Equal(_t.code.BETWEEN(L(1), (byte)10), "\"t\".code BETWEEN 1 AND :0", 1, (byte)10);

	[Fact]
	public void NumericExpr_BETWEEN_ByteAndLiteral_CorrectSql() =>
	_assert.Equal(_t.code.BETWEEN((byte)1, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, (byte)1);

	[Fact]
	public void NumericExpr_BETWEEN_ByteValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((byte)1, (byte)10), "\"t\".code BETWEEN :0 AND :1", 2, (byte)1, (byte)10);

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndShort_CorrectSql() =>
	_assert.Equal(_t.code.BETWEEN(L(1), (short)10), "\"t\".code BETWEEN 1 AND :0", 1, (short)10);

	[Fact]
	public void NumericExpr_BETWEEN_ShortAndLiteral_CorrectSql() =>
	_assert.Equal(_t.code.BETWEEN((short)1, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, (short)1);

	[Fact]
	public void NumericExpr_BETWEEN_ShortValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((short)1, (short)10), "\"t\".code BETWEEN :0 AND :1", 2, (short)1, (short)10);

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndUShort_CorrectSql() =>
	_assert.Equal(_t.code.BETWEEN(L(1), (ushort)10), "\"t\".code BETWEEN 1 AND :0", 1, (ushort)10);

	[Fact]
	public void NumericExpr_BETWEEN_UShortAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((ushort)1, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, (ushort)1);

	[Fact]
	public void NumericExpr_BETWEEN_UShortValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((ushort)1, (ushort)10), "\"t\".code BETWEEN :0 AND :1", 2, (ushort)1, (ushort)10);

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndInt_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(L(1), 10), "\"t\".code BETWEEN 1 AND :0", 1, 10);

	[Fact]
	public void NumericExpr_BETWEEN_IntAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(1, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, 1);

	[Fact]
	public void NumericExpr_BETWEEN_IntValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(1, 10), "\"t\".code BETWEEN :0 AND :1", 2, 1, 10);

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndUInt_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(L(1), (uint)10), "\"t\".code BETWEEN 1 AND :0", 1, (uint)10);

	[Fact]
	public void NumericExpr_BETWEEN_UIntAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((uint)1, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, (uint)1);

	[Fact]
	public void NumericExpr_BETWEEN_UIntValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((uint)1, (uint)10), "\"t\".code BETWEEN :0 AND :1", 2, (uint)1, (uint)10);

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndLong_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(L(1), (long)10), "\"t\".code BETWEEN 1 AND :0", 1, (long)10);

	[Fact]
	public void NumericExpr_BETWEEN_LongAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((long)1, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, (long)1);

	[Fact]
	public void NumericExpr_BETWEEN_LongValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((long)1, (long)10), "\"t\".code BETWEEN :0 AND :1", 2, (long)1, (long)10);

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndULong_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(L(1), (ulong)10), "\"t\".code BETWEEN 1 AND :0", 1, (ulong)10);

	[Fact]
	public void NumericExpr_BETWEEN_ULongAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((ulong)1, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, (ulong)1);

	[Fact]
	public void NumericExpr_BETWEEN_ULongValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN((ulong)1, (ulong)10), "\"t\".code BETWEEN :0 AND :1", 2, (ulong)1, (ulong)10);

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndFloat_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(L(1), 10.0f), "\"t\".code BETWEEN 1 AND :0", 1, 10.0f);

	[Fact]
	public void NumericExpr_BETWEEN_FloatAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(1.0f, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, 1.0f);

	[Fact]
	public void NumericExpr_BETWEEN_FloatValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(1.0f, 10.0f), "\"t\".code BETWEEN :0 AND :1", 2, 1.0f, 10.0f);

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndDouble_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(L(1), 10.0), "\"t\".code BETWEEN 1 AND :0", 1, 10.0);

	[Fact]
	public void NumericExpr_BETWEEN_DoubleAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(1.0, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, 1.0);

	[Fact]
	public void NumericExpr_BETWEEN_DoubleValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(1.0, 10.0), "\"t\".code BETWEEN :0 AND :1", 2, 1.0, 10.0);

	[Fact]
	public void NumericExpr_BETWEEN_LiteralAndDecimal_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(L(1), 10.0m), "\"t\".code BETWEEN 1 AND :0", 1, 10.0m);

	[Fact]
	public void NumericExpr_BETWEEN_DecimalAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(1.0m, L(10)), "\"t\".code BETWEEN :0 AND 10", 1, 1.0m);

	[Fact]
	public void NumericExpr_BETWEEN_DecimalValues_CorrectSql() =>
		_assert.Equal(_t.code.BETWEEN(1.0m, 10.0m), "\"t\".code BETWEEN :0 AND :1", 2, 1.0m, 10.0m);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_Literals_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), L(10)), "\"t\".code NOT BETWEEN 1 AND 10");

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndSByte_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), (sbyte)10), "\"t\".code NOT BETWEEN 1 AND :0", 1, (sbyte)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_SByteAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((sbyte)1, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, (sbyte)1);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_SByteValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((sbyte)1, (sbyte)10), "\"t\".code NOT BETWEEN :0 AND :1", 2, (sbyte)1, (sbyte)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndByte_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), (byte)10), "\"t\".code NOT BETWEEN 1 AND :0", 1, (byte)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_ByteAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((byte)1, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, (byte)1);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_ByteValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((byte)1, (byte)10), "\"t\".code NOT BETWEEN :0 AND :1", 2, (byte)1, (byte)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndShort_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), (short)10), "\"t\".code NOT BETWEEN 1 AND :0", 1, (short)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_ShortAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((short)1, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, (short)1);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_ShortValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((short)1, (short)10), "\"t\".code NOT BETWEEN :0 AND :1", 2, (short)1, (short)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndUShort_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), (ushort)10), "\"t\".code NOT BETWEEN 1 AND :0", 1, (ushort)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_UShortAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((ushort)1, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, (ushort)1);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_UShortValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((ushort)1, (ushort)10), "\"t\".code NOT BETWEEN :0 AND :1", 2, (ushort)1, (ushort)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndInt_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), 10), "\"t\".code NOT BETWEEN 1 AND :0", 1, 10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_IntAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(1, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, 1);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_IntValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(1, 10), "\"t\".code NOT BETWEEN :0 AND :1", 2, 1, 10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndUInt_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), (uint)10), "\"t\".code NOT BETWEEN 1 AND :0", 1, (uint)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_UIntAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((uint)1, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, (uint)1);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_UIntValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((uint)1, (uint)10), "\"t\".code NOT BETWEEN :0 AND :1", 2, (uint)1, (uint)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndLong_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), (long)10), "\"t\".code NOT BETWEEN 1 AND :0", 1, (long)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LongAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((long)1, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, (long)1);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LongValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((long)1, (long)10), "\"t\".code NOT BETWEEN :0 AND :1", 2, (long)1, (long)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndULong_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), (ulong)10), "\"t\".code NOT BETWEEN 1 AND :0", 1, (ulong)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_ULongAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((ulong)1, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, (ulong)1);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_ULongValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN((ulong)1, (ulong)10), "\"t\".code NOT BETWEEN :0 AND :1", 2, (ulong)1, (ulong)10);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndFloat_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), 10.0f), "\"t\".code NOT BETWEEN 1 AND :0", 1, 10.0f);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_FloatAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(1.0f, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, 1.0f);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_FloatValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(1.0f, 10.0f), "\"t\".code NOT BETWEEN :0 AND :1", 2, 1.0f, 10.0f);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndDouble_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), 10.0), "\"t\".code NOT BETWEEN 1 AND :0", 1, 10.0);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_DoubleAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(1.0, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, 1.0);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_DoubleValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(1.0, 10.0), "\"t\".code NOT BETWEEN :0 AND :1", 2, 1.0, 10.0);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_LiteralAndDecimal_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(L(1), 10.0m), "\"t\".code NOT BETWEEN 1 AND :0", 1, 10.0m);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_DecimalAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(1.0m, L(10)), "\"t\".code NOT BETWEEN :0 AND 10", 1, 1.0m);

	[Fact]
	public void NumericExpr_NOT_BETWEEN_DecimalValues_CorrectSql() =>
		_assert.Equal(_t.code.NOT_BETWEEN(1.0m, 10.0m), "\"t\".code NOT BETWEEN :0 AND :1", 2, 1.0m, 10.0m);
}
