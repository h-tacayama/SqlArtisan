using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class NumericExpr_ArithmeticTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void NumericExpr_Addition_Literals_CorrectSql() =>
        Assert.Equal("SELECT (1 + 2)", SELECT(L(1) + L(2)).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndSByte_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + (sbyte)2).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndByte_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + (byte)2).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndShort_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + (short)2).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndUShort_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + (ushort)2).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + 2).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndUInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + (uint)2).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndLong_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + (long)2).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndULong_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + (ulong)2).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndFloat_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + 2.0f).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndDouble_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + 2.0).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndDecimal_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + 2.0m).Build().Text);

    [Fact]
    public void NumericExpr_Addition_ColumnAndEnum_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code + :0)", SELECT(_t.code + TestEnum.Two).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_Literals_CorrectSql() =>
        Assert.Equal("SELECT (1 - 2)", SELECT(L(1) - L(2)).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndSByte_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - (sbyte)2).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndByte_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - (byte)2).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndShort_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - (short)2).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndUShort_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - (ushort)2).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - 2).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndUInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - (uint)2).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndLong_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - (long)2).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndULong_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - (ulong)2).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndFloat_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - 2.0f).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndDouble_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - 2.0).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndDecimal_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - 2.0m).Build().Text);

    [Fact]
    public void NumericExpr_Subtraction_ColumnAndEnum_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code - :0)", SELECT(_t.code - TestEnum.Two).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_Literals_CorrectSql() =>
        Assert.Equal("SELECT (1 * 2)", SELECT(L(1) * L(2)).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndSByte_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * (sbyte)2).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndByte_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * (byte)2).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndShort_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * (short)2).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndUShort_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * (ushort)2).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * 2).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndUInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * (uint)2).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndLong_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * (long)2).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndULong_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * (ulong)2).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndFloat_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * 2.0f).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndDouble_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * 2.0).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndDecimal_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * 2.0m).Build().Text);

    [Fact]
    public void NumericExpr_Multiplication_ColumnAndEnum_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code * :0)", SELECT(_t.code * TestEnum.Two).Build().Text);

    [Fact]
    public void NumericExpr_Division_Literals_CorrectSql() =>
        Assert.Equal("SELECT (1 / 2)", SELECT(L(1) / L(2)).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndSByte_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / (sbyte)2).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndByte_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / (byte)2).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndShort_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / (short)2).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndUShort_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / (ushort)2).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / 2).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndUInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / (uint)2).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndLong_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / (long)2).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndULong_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / (ulong)2).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndFloat_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / 2.0f).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndDouble_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / 2.0).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndDecimal_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / 2.0m).Build().Text);

    [Fact]
    public void NumericExpr_Division_ColumnAndEnum_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code / :0)", SELECT(_t.code / TestEnum.Two).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_Literals_CorrectSql() =>
        Assert.Equal("SELECT (1 % 2)", SELECT(L(1) % L(2)).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndSByte_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % (sbyte)2).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndByte_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % (byte)2).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndShort_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % (short)2).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndUShort_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % (ushort)2).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % 2).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndUInt_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % (uint)2).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndLong_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % (long)2).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndULong_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % (ulong)2).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndFloat_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % 2.0f).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndDouble_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % 2.0).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndDecimal_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % 2.0m).Build().Text);

    [Fact]
    public void NumericExpr_Modulus_ColumnAndEnum_CorrectSql() =>
        Assert.Equal("SELECT (\"t\".code % :0)", SELECT(_t.code % TestEnum.Two).Build().Text);
}
