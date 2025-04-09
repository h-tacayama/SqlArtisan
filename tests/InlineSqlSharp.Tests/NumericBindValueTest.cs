using System.Data;

namespace InlineSqlSharp.Tests;

public class NumericBindValueTest
{
    [Fact]
    public void NumericBindValue_SByte_CorrectDbType()
    {
        NumericBindValue<sbyte> bindValue = new(1);
        Assert.Equal((sbyte)1, bindValue.Value);
        Assert.Equal(DbType.SByte, bindValue.DbType);
    }

    [Fact]
    public void NumericBindValue_Byte_CorrectDbType()
    {
        NumericBindValue<byte> bindValue = new(1);
        Assert.Equal((byte)1, bindValue.Value);
        Assert.Equal(DbType.Byte, bindValue.DbType);
    }

    [Fact]
    public void NumericBindValue_Short_CorrectDbType()
    {
        NumericBindValue<short> bindValue = new(1);
        Assert.Equal((short)1, bindValue.Value);
        Assert.Equal(DbType.Int16, bindValue.DbType);
    }

    [Fact]
    public void NumericBindValue_UShort_CorrectDbType()
    {
        NumericBindValue<ushort> bindValue = new(1);
        Assert.Equal((ushort)1, bindValue.Value);
        Assert.Equal(DbType.UInt16, bindValue.DbType);
    }

    [Fact]
    public void NumericBindValue_Int_CorrectDbType()
    {
        NumericBindValue<int> bindValue = new(1);
        Assert.Equal(1, bindValue.Value);
        Assert.Equal(DbType.Int32, bindValue.DbType);
    }

    [Fact]
    public void NumericBindValue_UInt_CorrectDbType()
    {
        NumericBindValue<uint> bindValue = new(1);
        Assert.Equal((uint)1, bindValue.Value);
        Assert.Equal(DbType.UInt32, bindValue.DbType);
    }

    [Fact]
    public void NumericBindValue_Long_CorrectDbType()
    {
        NumericBindValue<long> bindValue = new(1);
        Assert.Equal((long)1, bindValue.Value);
        Assert.Equal(DbType.Int64, bindValue.DbType);
    }

    [Fact]
    public void NumericBindValue_ULong_CorrectDbType()
    {
        NumericBindValue<ulong> bindValue = new(1);
        Assert.Equal((ulong)1, bindValue.Value);
        Assert.Equal(DbType.UInt64, bindValue.DbType);
    }

    [Fact]
    public void NumericBindValue_Float_CorrectDbType()
    {
        NumericBindValue<float> bindValue = new(1);
        Assert.Equal((float)1, bindValue.Value);
        Assert.Equal(DbType.Single, bindValue.DbType);
    }

    [Fact]
    public void NumericBindValue_Double_CorrectDbType()
    {
        NumericBindValue<double> bindValue = new(1);
        Assert.Equal((double)1, bindValue.Value);
        Assert.Equal(DbType.Double, bindValue.DbType);
    }

    [Fact]
    public void NumericBindValue_Decimal_CorrectDbType()
    {
        NumericBindValue<decimal> bindValue = new(1);
        Assert.Equal((decimal)1, bindValue.Value);
        Assert.Equal(DbType.Decimal, bindValue.DbType);
    }
}
