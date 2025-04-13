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
                        ((byte)1, (sbyte)2),
                        ((short)3, (ushort)4),
                        ((int)5, (uint)6),
                        ((long)7, (ulong)8),
                        ((float)9, (double)10),
                    ],
                    (decimal)11))
            .Build();

        Assert.Equal(14, sql.Parameters.Count);
        Assert.IsType<DateTime>(sql.Parameters[0].Value);
        Assert.IsType<string>(sql.Parameters[1].Value);
        Assert.IsType<string>(sql.Parameters[2].Value);
        Assert.IsType<byte>(sql.Parameters[3].Value);
        Assert.IsType<sbyte>(sql.Parameters[4].Value);
        Assert.IsType<short>(sql.Parameters[5].Value);
        Assert.IsType<ushort>(sql.Parameters[6].Value);
        Assert.IsType<int>(sql.Parameters[7].Value);
        Assert.IsType<uint>(sql.Parameters[8].Value);
        Assert.IsType<long>(sql.Parameters[9].Value);
        Assert.IsType<ulong>(sql.Parameters[10].Value);
        Assert.IsType<float>(sql.Parameters[11].Value);
        Assert.IsType<double>(sql.Parameters[12].Value);
        Assert.IsType<decimal>(sql.Parameters[13].Value);

        Assert.Equal(new DateTime(2000, 1, 2), sql.Parameters[0].Value);
        Assert.Equal("a", sql.Parameters[1].Value);
        Assert.Equal("b", sql.Parameters[2].Value);
        Assert.Equal((byte)1, sql.Parameters[3].Value);
        Assert.Equal((sbyte)2, sql.Parameters[4].Value);
        Assert.Equal((short)3, sql.Parameters[5].Value);
        Assert.Equal((ushort)4, sql.Parameters[6].Value);
        Assert.Equal((int)5, sql.Parameters[7].Value);
        Assert.Equal((uint)6, sql.Parameters[8].Value);
        Assert.Equal((long)7, sql.Parameters[9].Value);
        Assert.Equal((ulong)8, sql.Parameters[10].Value);
        Assert.Equal((float)9, sql.Parameters[11].Value);
        Assert.Equal((double)10, sql.Parameters[12].Value);
        Assert.Equal((decimal)11, sql.Parameters[13].Value);
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
