using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ExpressionsTest
{
    [Fact]
    public void Select_BindParameters_CorrectSql()
    {
        SqlStatement sql =
            Select(
                'a',
                "O''Reilly",
                new DateTime(2001, 2, 3),
                (sbyte)1,
                (byte)2,
                (short)3,
                (ushort)4,
                (int)5,
                (uint)6,
                (long)7,
                (ulong)8,
                (float)9.9,
                (double)10.10,
                (decimal)11.11,
                true,
                false)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2, ");
        expected.Append(":3, ");
        expected.Append(":4, ");
        expected.Append(":5, ");
        expected.Append(":6, ");
        expected.Append(":7, ");
        expected.Append(":8, ");
        expected.Append(":9, ");
        expected.Append(":10, ");
        expected.Append(":11, ");
        expected.Append(":12, ");
        expected.Append(":13, ");
        expected.Append(":14, ");
        expected.Append(":15");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal('a', sql.Parameters.Get<char>(":0"));
        Assert.Equal("O''Reilly", sql.Parameters.Get<string>(":1"));
        Assert.Equal(new DateTime(2001, 2, 3), sql.Parameters.Get<DateTime>(":2"));
        Assert.Equal((sbyte)1, sql.Parameters.Get<sbyte>(":3"));
        Assert.Equal((byte)2, sql.Parameters.Get<byte>(":4"));
        Assert.Equal((short)3, sql.Parameters.Get<short>(":5"));
        Assert.Equal((ushort)4, sql.Parameters.Get<ushort>(":6"));
        Assert.Equal((int)5, sql.Parameters.Get<int>(":7"));
        Assert.Equal((uint)6, sql.Parameters.Get<uint>(":8"));
        Assert.Equal((long)7, sql.Parameters.Get<long>(":9"));
        Assert.Equal((ulong)8, sql.Parameters.Get<ulong>(":10"));
        Assert.Equal((float)9.9, sql.Parameters.Get<float>(":11"));
        Assert.Equal((double)10.1, sql.Parameters.Get<double>(":12"));
        Assert.Equal((decimal)11.11, sql.Parameters.Get<decimal>(":13"));
        Assert.True(sql.Parameters.Get<bool>(":14"));
        Assert.False(sql.Parameters.Get<bool>(":15"));
    }

    [Fact]
    public void Select_Null_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Null,
                Null.As("e"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT NULL, NULL \"e\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Select_SequenceValues_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Sequence("seq").Currval,
                Sequence("seq").Nextval)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("seq.CURRVAL, ");
        expected.Append("seq.NEXTVAL");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
