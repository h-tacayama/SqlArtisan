using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class BindValueFactoryTest
{
    [Fact]
    public void CreateOrException_WithVariousTypes_ReturnsCorrectBindValues()
    {
        SqlStatement sql =
            SELECT(
                DECODE(
                    new DateTime(2000, 1, 2),
                    [
                        ('a', "b"),
                        ((sbyte)1, (byte)2),
                        ((short)3, (ushort)4),
                        ((int)5, (uint)6),
                        ((long)7, (ulong)8),
                        ((float)9, (double)10),
                    ],
                    (decimal)11))
            .Build();

        Assert.Equal(14, sql.ParameterCount);
        Assert.Equal(new DateTime(2000, 1, 2), sql.Parameters.Get<DateTime>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal("b", sql.Parameters.Get<string>(":2"));
        Assert.Equal((sbyte)1, sql.Parameters.Get<sbyte>(":3"));
        Assert.Equal((byte)2, sql.Parameters.Get<byte>(":4"));
        Assert.Equal((short)3, sql.Parameters.Get<short>(":5"));
        Assert.Equal((ushort)4, sql.Parameters.Get<ushort>(":6"));
        Assert.Equal((int)5, sql.Parameters.Get<int>(":7"));
        Assert.Equal((uint)6, sql.Parameters.Get<uint>(":8"));
        Assert.Equal((long)7, sql.Parameters.Get<long>(":9"));
        Assert.Equal((ulong)8, sql.Parameters.Get<ulong>(":10"));
        Assert.Equal((float)9, sql.Parameters.Get<float>(":11"));
        Assert.Equal((double)10, sql.Parameters.Get<double>(":12"));
        Assert.Equal((decimal)11, sql.Parameters.Get<decimal>(":13"));
    }

    [Fact]
    public void CreateOrException_WithInvalidValue_ThrowsArgumentException()
    {
        Assert.Throws<NotSupportedException>(() =>
            SELECT(
                DECODE(
                    new test_table("a"),
                    [
                        (1, 2),
                    ],
                    999))
            .Build());
    }
}
