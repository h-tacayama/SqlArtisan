using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class NumericExpr_ComparisonTest
{
	private readonly test_table _t;
	private readonly ConditionTestAssert _assert;

	private enum SByteEnum : sbyte { One = 1, }
	private enum ByteEnum : byte { One = 1, }
	private enum ShortEnum : short { One = 1, }
	private enum UshortEnum : ushort { One = 1, }
	private enum UintEnum : uint { One = 1, }
	private enum LongEnum : long { One = 1, }
	private enum UlongEnum : ulong { One = 1, }

	public NumericExpr_ComparisonTest()
	{
		_t = new test_table("t");
		_assert = new(_t);
	}

	[Fact]
	public void NumericExpr_Equal_ColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code == L(1), "\"t\".code = 1");

	[Fact]
	public void NumericExpr_Equal_ColumnAndSByte_CorrectSql() =>
		_assert.Equal(_t.code == (sbyte)1, "\"t\".code = :0", 1, (sbyte)1);

	[Fact]
	public void NumericExpr_Equal_ColumnAndByte_CorrectSql() =>
		_assert.Equal(_t.code == (byte)1, "\"t\".code = :0", 1, (byte)1);

	[Fact]
	public void NumericExpr_Equal_ColumnAndShort_CorrectSql() =>
		_assert.Equal(_t.code == (short)1, "\"t\".code = :0", 1, (short)1);

	[Fact]
	public void NumericExpr_Equal_ColumnAndUShort_CorrectSql() =>
		_assert.Equal(_t.code == (ushort)1, "\"t\".code = :0", 1, (ushort)1);

	[Fact]
	public void NumericExpr_Equal_ColumnAndInt_CorrectSql() =>
		_assert.Equal(_t.code == 1, "\"t\".code = :0", 1, 1);

	[Fact]
	public void NumericExpr_Equal_ColumnAndUInt_CorrectSql() =>
		_assert.Equal(_t.code == (uint)1, "\"t\".code = :0", 1, (uint)1);

	[Fact]
	public void NumericExpr_Equal_ColumnAndLong_CorrectSql() =>
		_assert.Equal(_t.code == (long)1, "\"t\".code = :0", 1, (long)1);

	[Fact]
	public void NumericExpr_Equal_ColumnAndULong_CorrectSql() =>
		_assert.Equal(_t.code == (ulong)1, "\"t\".code = :0", 1, (ulong)1);

	[Fact]
	public void NumericExpr_Equal_ColumnAndFloat_CorrectSql() =>
		_assert.Equal(_t.code == 1.0f, "\"t\".code = :0", 1, 1.0f);

	[Fact]
	public void NumericExpr_Equal_ColumnAndDouble_CorrectSql() =>
		_assert.Equal(_t.code == 1.0, "\"t\".code = :0", 1, 1.0);

	[Fact]
	public void NumericExpr_Equal_ColumnAndDecimal_CorrectSql() =>
		_assert.Equal(_t.code == 1.0m, "\"t\".code = :0", 1, 1.0m);

	[Fact]
	public void NumericExpr_Equal_ColumnAndSbyteEnum_CorrectSql() =>
		_assert.Equal(_t.code == SByteEnum.One, "\"t\".code = :0", 1, (sbyte)SByteEnum.One);

	[Fact]
	public void NumericExpr_Equal_ColumnAndByteEnum_CorrectSql() =>
		_assert.Equal(_t.code == ByteEnum.One, "\"t\".code = :0", 1, (byte)ByteEnum.One);

	[Fact]
	public void NumericExpr_Equal_ColumnAndShortEnum_CorrectSql() =>
		_assert.Equal(_t.code == ShortEnum.One, "\"t\".code = :0", 1, (short)ShortEnum.One);

	[Fact]
	public void NumericExpr_Equal_ColumnAndUshortEnum_CorrectSql() =>
		_assert.Equal(_t.code == UshortEnum.One, "\"t\".code = :0", 1, (ushort)UshortEnum.One);

	[Fact]
	public void NumericExpr_Equal_ColumnAndIntEnum_CorrectSql() =>
		_assert.Equal(_t.code == TestEnum.One, "\"t\".code = :0", 1, (int)TestEnum.One);

	[Fact]
	public void NumericExpr_Equal_ColumnAndUintEnum_CorrectSql() =>
		_assert.Equal(_t.code == UintEnum.One, "\"t\".code = :0", 1, (uint)UintEnum.One);

	[Fact]
	public void NumericExpr_Equal_ColumnAndLongEnum_CorrectSql() =>
		_assert.Equal(_t.code == LongEnum.One, "\"t\".code = :0", 1, (long)LongEnum.One);

	[Fact]
	public void NumericExpr_Equal_ColumnAndUlongEnum_CorrectSql() =>
		_assert.Equal(_t.code == UlongEnum.One, "\"t\".code = :0", 1, (ulong)UlongEnum.One);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code != L(1), "\"t\".code <> 1");

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndSByte_CorrectSql() =>
		_assert.Equal(_t.code != (sbyte)1, "\"t\".code <> :0", 1, (sbyte)1);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndByte_CorrectSql() =>
		_assert.Equal(_t.code != (byte)1, "\"t\".code <> :0", 1, (byte)1);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndShort_CorrectSql() =>
		_assert.Equal(_t.code != (short)1, "\"t\".code <> :0", 1, (short)1);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndUShort_CorrectSql() =>
		_assert.Equal(_t.code != (ushort)1, "\"t\".code <> :0", 1, (ushort)1);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndInt_CorrectSql() =>
		_assert.Equal(_t.code != 1, "\"t\".code <> :0", 1, 1);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndUInt_CorrectSql() =>
		_assert.Equal(_t.code != (uint)1, "\"t\".code <> :0", 1, (uint)1);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndLong_CorrectSql() =>
		_assert.Equal(_t.code != (long)1, "\"t\".code <> :0", 1, (long)1);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndULong_CorrectSql() =>
		_assert.Equal(_t.code != (ulong)1, "\"t\".code <> :0", 1, (ulong)1);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndFloat_CorrectSql() =>
		_assert.Equal(_t.code != 1.0f, "\"t\".code <> :0", 1, 1.0f);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndDouble_CorrectSql() =>
		_assert.Equal(_t.code != 1.0, "\"t\".code <> :0", 1, 1.0);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndDecimal_CorrectSql() =>
		_assert.Equal(_t.code != 1.0m, "\"t\".code <> :0", 1, 1.0m);

	[Fact]
	public void NumericExpr_NotEqual_ColumnAndEnum_CorrectSql() =>
		_assert.Equal(_t.code != TestEnum.One, "\"t\".code <> :0", 1, (int)TestEnum.One);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code < L(1), "\"t\".code < 1");

	[Fact]
	public void NumericExpr_LessThan_ColumnAndSByte_CorrectSql() =>
		_assert.Equal(_t.code < (sbyte)1, "\"t\".code < :0", 1, (sbyte)1);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndByte_CorrectSql() =>
		_assert.Equal(_t.code < (byte)1, "\"t\".code < :0", 1, (byte)1);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndShort_CorrectSql() =>
		_assert.Equal(_t.code < (short)1, "\"t\".code < :0", 1, (short)1);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndUShort_CorrectSql() =>
		_assert.Equal(_t.code < (ushort)1, "\"t\".code < :0", 1, (ushort)1);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndInt_CorrectSql() =>
		_assert.Equal(_t.code < 1, "\"t\".code < :0", 1, 1);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndUInt_CorrectSql() =>
		_assert.Equal(_t.code < (uint)1, "\"t\".code < :0", 1, (uint)1);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndLong_CorrectSql() =>
		_assert.Equal(_t.code < (long)1, "\"t\".code < :0", 1, (long)1);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndULong_CorrectSql() =>
		_assert.Equal(_t.code < (ulong)1, "\"t\".code < :0", 1, (ulong)1);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndFloat_CorrectSql() =>
		_assert.Equal(_t.code < 1.0f, "\"t\".code < :0", 1, 1.0f);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndDouble_CorrectSql() =>
		_assert.Equal(_t.code < 1.0, "\"t\".code < :0", 1, 1.0);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndDecimal_CorrectSql() =>
		_assert.Equal(_t.code < 1.0m, "\"t\".code < :0", 1, 1.0m);

	[Fact]
	public void NumericExpr_LessThan_ColumnAndEnum_CorrectSql() =>
		_assert.Equal(_t.code < TestEnum.One, "\"t\".code < :0", 1, (int)TestEnum.One);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code > L(1), "\"t\".code > 1");

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndSByte_CorrectSql() =>
		_assert.Equal(_t.code > (sbyte)1, "\"t\".code > :0", 1, (sbyte)1);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndByte_CorrectSql() =>
		_assert.Equal(_t.code > (byte)1, "\"t\".code > :0", 1, (byte)1);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndShort_CorrectSql() =>
		_assert.Equal(_t.code > (short)1, "\"t\".code > :0", 1, (short)1);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndUShort_CorrectSql() =>
		_assert.Equal(_t.code > (ushort)1, "\"t\".code > :0", 1, (ushort)1);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndInt_CorrectSql() =>
		_assert.Equal(_t.code > 1, "\"t\".code > :0", 1, 1);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndUInt_CorrectSql() =>
		_assert.Equal(_t.code > (uint)1, "\"t\".code > :0", 1, (uint)1);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndLong_CorrectSql() =>
		_assert.Equal(_t.code > (long)1, "\"t\".code > :0", 1, (long)1);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndULong_CorrectSql() =>
		_assert.Equal(_t.code > (ulong)1, "\"t\".code > :0", 1, (ulong)1);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndFloat_CorrectSql() =>
		_assert.Equal(_t.code > 1.0f, "\"t\".code > :0", 1, 1.0f);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndDouble_CorrectSql() =>
		_assert.Equal(_t.code > 1.0, "\"t\".code > :0", 1, 1.0);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndDecimal_CorrectSql() =>
		_assert.Equal(_t.code > 1.0m, "\"t\".code > :0", 1, 1.0m);

	[Fact]
	public void NumericExpr_GreaterThan_ColumnAndEnum_CorrectSql() =>
		_assert.Equal(_t.code > TestEnum.One, "\"t\".code > :0", 1, (int)TestEnum.One);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code <= L(1), "\"t\".code <= 1");

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndSByte_CorrectSql() =>
		_assert.Equal(_t.code <= (sbyte)1, "\"t\".code <= :0", 1, (sbyte)1);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndByte_CorrectSql() =>
		_assert.Equal(_t.code <= (byte)1, "\"t\".code <= :0", 1, (byte)1);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndShort_CorrectSql() =>
		_assert.Equal(_t.code <= (short)1, "\"t\".code <= :0", 1, (short)1);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndUShort_CorrectSql() =>
		_assert.Equal(_t.code <= (ushort)1, "\"t\".code <= :0", 1, (ushort)1);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndInt_CorrectSql() =>
		_assert.Equal(_t.code <= 1, "\"t\".code <= :0", 1, 1);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndUInt_CorrectSql() =>
		_assert.Equal(_t.code <= (uint)1, "\"t\".code <= :0", 1, (uint)1);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndLong_CorrectSql() =>
		_assert.Equal(_t.code <= (long)1, "\"t\".code <= :0", 1, (long)1);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndULong_CorrectSql() =>
		_assert.Equal(_t.code <= (ulong)1, "\"t\".code <= :0", 1, (ulong)1);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndFloat_CorrectSql() =>
		_assert.Equal(_t.code <= 1.0f, "\"t\".code <= :0", 1, 1.0f);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndDouble_CorrectSql() =>
		_assert.Equal(_t.code <= 1.0, "\"t\".code <= :0", 1, 1.0);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndDecimal_CorrectSql() =>
		_assert.Equal(_t.code <= 1.0m, "\"t\".code <= :0", 1, 1.0m);

	[Fact]
	public void NumericExpr_LessEqual_ColumnAndEnum_CorrectSql() =>
		_assert.Equal(_t.code <= TestEnum.One, "\"t\".code <= :0", 1, (int)TestEnum.One);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndLiteral_CorrectSql() =>
		_assert.Equal(_t.code >= L(1), "\"t\".code >= 1");

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndSByte_CorrectSql() =>
		_assert.Equal(_t.code >= (sbyte)1, "\"t\".code >= :0", 1, (sbyte)1);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndByte_CorrectSql() =>
		_assert.Equal(_t.code >= (byte)1, "\"t\".code >= :0", 1, (byte)1);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndShort_CorrectSql() =>
		_assert.Equal(_t.code >= (short)1, "\"t\".code >= :0", 1, (short)1);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndUShort_CorrectSql() =>
		_assert.Equal(_t.code >= (ushort)1, "\"t\".code >= :0", 1, (ushort)1);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndInt_CorrectSql() =>
		_assert.Equal(_t.code >= 1, "\"t\".code >= :0", 1, 1);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndUInt_CorrectSql() =>
		_assert.Equal(_t.code >= (uint)1, "\"t\".code >= :0", 1, (uint)1);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndLong_CorrectSql() =>
		_assert.Equal(_t.code >= (long)1, "\"t\".code >= :0", 1, (long)1);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndULong_CorrectSql() =>
		_assert.Equal(_t.code >= (ulong)1, "\"t\".code >= :0", 1, (ulong)1);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndFloat_CorrectSql() =>
		_assert.Equal(_t.code >= 1.0f, "\"t\".code >= :0", 1, 1.0f);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndDouble_CorrectSql() =>
		_assert.Equal(_t.code >= 1.0, "\"t\".code >= :0", 1, 1.0);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndDecimal_CorrectSql() =>
		_assert.Equal(_t.code >= 1.0m, "\"t\".code >= :0", 1, 1.0m);

	[Fact]
	public void NumericExpr_GreaterEqual_ColumnAndEnum_CorrectSql() =>
		_assert.Equal(_t.code >= TestEnum.One, "\"t\".code >= :0", 1, (int)TestEnum.One);
}
