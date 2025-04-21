using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class ExprRsolverTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void Resolve_WithColumn_DoesNotBindParameter()
    {
        SqlStatement sql = SELECT(ABS(_t.code)).Build();

        Assert.Equal(0, sql.ParameterCount);
        Assert.Equal("SELECT ABS(\"t\".code)", sql.Text);
    }

    [Fact]
    public void Resolve_WithCharacterLiteral_BindsAsCharacter()
    {
        SqlStatement sql =
            SELECT(
                ABS('a'),
                ABS("b"))
            .Build();

        Assert.Equal('a', sql.Parameters.Get<char>(":0"));
        Assert.Equal("b", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void Resolve_WithDateTimeLiteral_BindsAsDateTime()
    {
        SqlStatement sql =
            SELECT(
                ABS(new DateTime(2001, 2, 3)),
                ABS(new DateOnly(2004, 5, 6)),
                ABS(new TimeOnly(7, 8, 9)))
            .Build();

        Assert.Equal(new DateTime(2001, 2, 3), sql.Parameters.Get<DateTime>(":0"));
        Assert.Equal(new DateOnly(2004, 5, 6), sql.Parameters.Get<DateOnly>(":1"));
        Assert.Equal(new TimeOnly(7, 8, 9), sql.Parameters.Get<TimeOnly>(":2"));
    }

    [Fact]
    public void Resolve_WithNumberLiteral_BindsAsNumber()
    {
        SqlStatement sql =
            SELECT(
                ABS((sbyte)1),
                ABS((byte)2),
                ABS((short)3),
                ABS((ushort)4),
                ABS((int)5),
                ABS((uint)6),
                ABS((long)7),
                ABS((ulong)8),
                ABS((float)9.9),
                ABS((double)10.10),
                ABS((decimal)11.11))
            .Build();

        Assert.Equal((sbyte)1, sql.Parameters.Get<sbyte>(":0"));
        Assert.Equal((byte)2, sql.Parameters.Get<byte>(":1"));
        Assert.Equal((short)3, sql.Parameters.Get<short>(":2"));
        Assert.Equal((ushort)4, sql.Parameters.Get<ushort>(":3"));
        Assert.Equal((int)5, sql.Parameters.Get<int>(":4"));
        Assert.Equal((uint)6, sql.Parameters.Get<uint>(":5"));
        Assert.Equal((long)7, sql.Parameters.Get<long>(":6"));
        Assert.Equal((ulong)8, sql.Parameters.Get<ulong>(":7"));
        Assert.Equal((float)9.9, sql.Parameters.Get<float>(":8"));
        Assert.Equal((double)10.1, sql.Parameters.Get<double>(":9"));
        Assert.Equal((decimal)11.11, sql.Parameters.Get<decimal>(":10"));
    }

    [Fact]
    public void Resolve_WithSortOrder_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            SqlStatement sql = SELECT(_t.code.ASC).Build();
        });
    }
}
