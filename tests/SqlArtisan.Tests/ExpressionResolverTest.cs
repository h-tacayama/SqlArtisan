using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ExpressionResolverTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void Resolve_WithColumn_DoesNotBindParameter()
    {
        SqlStatement sql = Select(Abs(_t.Code)).Build();

        Assert.Equal(0, sql.Parameters.Count);
        Assert.Equal("SELECT ABS(\"t\".code)", sql.Text);
    }

    [Fact]
    public void Resolve_WithCharacterLiteral_BindsAsCharacter()
    {
        SqlStatement sql =
            Select(
                Abs('a'),
                Abs("b"))
            .Build();

        Assert.Equal('a', sql.Parameters.Get<char>(":0"));
        Assert.Equal("b", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void Resolve_WithDateTimeLiteral_BindsAsDateTime()
    {
        SqlStatement sql =
            Select(
                Abs(new DateTime(2001, 2, 3)),
                Abs(new DateOnly(2004, 5, 6)),
                Abs(new TimeOnly(7, 8, 9)))
            .Build();

        Assert.Equal(new DateTime(2001, 2, 3), sql.Parameters.Get<DateTime>(":0"));
        Assert.Equal(new DateOnly(2004, 5, 6), sql.Parameters.Get<DateOnly>(":1"));
        Assert.Equal(new TimeOnly(7, 8, 9), sql.Parameters.Get<TimeOnly>(":2"));
    }

    [Fact]
    public void Resolve_WithNumberLiteral_BindsAsNumber()
    {
        SqlStatement sql =
            Select(
                Abs((sbyte)1),
                Abs((byte)2),
                Abs((short)3),
                Abs((ushort)4),
                Abs((int)5),
                Abs((uint)6),
                Abs((long)7),
                Abs((ulong)8),
                Abs((float)9.9),
                Abs((double)10.10),
                Abs((decimal)11.11))
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
            SqlStatement sql = Select(_t.Code.Asc).Build();
        });
    }
}
