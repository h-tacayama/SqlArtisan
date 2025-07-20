using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Datepart_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Datepart(Datepart.Year, CurrentTimestamp),
                Datepart(Datepart.Quarter, CurrentTimestamp),
                Datepart(Datepart.Month, CurrentTimestamp),
                Datepart(Datepart.Dayofyear, CurrentTimestamp),
                Datepart(Datepart.Day, CurrentTimestamp),
                Datepart(Datepart.Week, CurrentTimestamp),
                Datepart(Datepart.Weekday, CurrentTimestamp),
                Datepart(Datepart.Hour, CurrentTimestamp),
                Datepart(Datepart.Minute, CurrentTimestamp),
                Datepart(Datepart.Second, CurrentTimestamp),
                Datepart(Datepart.Millisecond, CurrentTimestamp),
                Datepart(Datepart.Microsecond, CurrentTimestamp),
                Datepart(Datepart.Nanosecond, CurrentTimestamp),
                Datepart(Datepart.Tzoffset, CurrentTimestamp),
                Datepart(Datepart.IsoWeek, CurrentTimestamp))
            .Build();

        StringBuilder expected = new StringBuilder()
            .Append("SELECT ")
            .Append("DATEPART(YEAR, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(QUARTER, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(MONTH, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(DAYOFYEAR, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(DAY, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(WEEK, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(WEEKDAY, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(HOUR, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(MINUTE, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(SECOND, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(MILLISECOND, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(MICROSECOND, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(NANOSECOND, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(TZOFFSET, CURRENT_TIMESTAMP), ")
            .Append("DATEPART(ISO_WEEK, CURRENT_TIMESTAMP)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Decode_WithIntDefault_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    [
                        (1, 10),
                        (2, 20),
                    ],
                    999))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal(10, sql.Parameters.Get<int>(":1"));
        Assert.Equal(2, sql.Parameters.Get<int>(":2"));
        Assert.Equal(20, sql.Parameters.Get<int>(":3"));
        Assert.Equal(999, sql.Parameters.Get<int>(":4"));
    }

    [Fact]
    public void Decode_WithDoubleDefault_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    [
                        (1, Null),
                        (Null, 20),
                    ],
                    999.9))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, NULL, NULL, :1, :2)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal(20, sql.Parameters.Get<int>(":1"));
        Assert.Equal(999.9, sql.Parameters.Get<double>(":2"));
    }

    [Fact]
    public void Decode_OnePair_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    (1, "a"),
                    "z"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal("z", sql.Parameters.Get<string>(":2"));
    }

    [Fact]
    public void Decode_TwoPairs_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    (1, "a"),
                    (2, "b"),
                    "z"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal(2, sql.Parameters.Get<int>(":2"));
        Assert.Equal("b", sql.Parameters.Get<string>(":3"));
        Assert.Equal("z", sql.Parameters.Get<string>(":4"));
    }

    [Fact]
    public void Decode_ThreePairs_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    (1, "a"),
                    (2, "b"),
                    (3, "c"),
                    "z"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4, :5, :6)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal(2, sql.Parameters.Get<int>(":2"));
        Assert.Equal("b", sql.Parameters.Get<string>(":3"));
        Assert.Equal(3, sql.Parameters.Get<int>(":4"));
        Assert.Equal("c", sql.Parameters.Get<string>(":5"));
        Assert.Equal("z", sql.Parameters.Get<string>(":6"));
    }

    [Fact]
    public void Decode_FourPairs_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    (1, "a"),
                    (2, "b"),
                    (3, "c"),
                    (4, "d"),
                    "z"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4, :5, :6, :7, :8)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal(2, sql.Parameters.Get<int>(":2"));
        Assert.Equal("b", sql.Parameters.Get<string>(":3"));
        Assert.Equal(3, sql.Parameters.Get<int>(":4"));
        Assert.Equal("c", sql.Parameters.Get<string>(":5"));
        Assert.Equal(4, sql.Parameters.Get<int>(":6"));
        Assert.Equal("d", sql.Parameters.Get<string>(":7"));
        Assert.Equal("z", sql.Parameters.Get<string>(":8"));
    }

    [Fact]
    public void Decode_FivePairs_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    (1, "a"),
                    (2, "b"),
                    (3, "c"),
                    (4, "d"),
                    (5, "e"),
                    "z"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4, :5, :6, :7, :8, :9, :10)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal(2, sql.Parameters.Get<int>(":2"));
        Assert.Equal("b", sql.Parameters.Get<string>(":3"));
        Assert.Equal(3, sql.Parameters.Get<int>(":4"));
        Assert.Equal("c", sql.Parameters.Get<string>(":5"));
        Assert.Equal(4, sql.Parameters.Get<int>(":6"));
        Assert.Equal("d", sql.Parameters.Get<string>(":7"));
        Assert.Equal(5, sql.Parameters.Get<int>(":8"));
        Assert.Equal("e", sql.Parameters.Get<string>(":9"));
        Assert.Equal("z", sql.Parameters.Get<string>(":10"));
    }

    [Fact]
    public void Decode_SixPairs_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    (1, "a"),
                    (2, "b"),
                    (3, "c"),
                    (4, "d"),
                    (5, "e"),
                    (6, "f"),
                    "z"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4, :5, :6, :7, :8, :9, :10, :11, :12)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal(2, sql.Parameters.Get<int>(":2"));
        Assert.Equal("b", sql.Parameters.Get<string>(":3"));
        Assert.Equal(3, sql.Parameters.Get<int>(":4"));
        Assert.Equal("c", sql.Parameters.Get<string>(":5"));
        Assert.Equal(4, sql.Parameters.Get<int>(":6"));
        Assert.Equal("d", sql.Parameters.Get<string>(":7"));
        Assert.Equal(5, sql.Parameters.Get<int>(":8"));
        Assert.Equal("e", sql.Parameters.Get<string>(":9"));
        Assert.Equal(6, sql.Parameters.Get<int>(":10"));
        Assert.Equal("f", sql.Parameters.Get<string>(":11"));
        Assert.Equal("z", sql.Parameters.Get<string>(":12"));
    }

    [Fact]
    public void Decode_SevenPairs_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    (1, "a"),
                    (2, "b"),
                    (3, "c"),
                    (4, "d"),
                    (5, "e"),
                    (6, "f"),
                    (7, "g"),
                    "z"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4, :5, :6, :7, :8, :9, :10, :11, :12, :13, :14)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal(2, sql.Parameters.Get<int>(":2"));
        Assert.Equal("b", sql.Parameters.Get<string>(":3"));
        Assert.Equal(3, sql.Parameters.Get<int>(":4"));
        Assert.Equal("c", sql.Parameters.Get<string>(":5"));
        Assert.Equal(4, sql.Parameters.Get<int>(":6"));
        Assert.Equal("d", sql.Parameters.Get<string>(":7"));
        Assert.Equal(5, sql.Parameters.Get<int>(":8"));
        Assert.Equal("e", sql.Parameters.Get<string>(":9"));
        Assert.Equal(6, sql.Parameters.Get<int>(":10"));
        Assert.Equal("f", sql.Parameters.Get<string>(":11"));
        Assert.Equal(7, sql.Parameters.Get<int>(":12"));
        Assert.Equal("g", sql.Parameters.Get<string>(":13"));
        Assert.Equal("z", sql.Parameters.Get<string>(":14"));
    }

    [Fact]
    public void Decode_EightPairs_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    (1, "a"),
                    (2, "b"),
                    (3, "c"),
                    (4, "d"),
                    (5, "e"),
                    (6, "f"),
                    (7, "g"),
                    (8, "h"),
                    "z"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4, :5, :6, :7, :8, :9, :10, :11, :12, :13, :14, :15, :16)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal(2, sql.Parameters.Get<int>(":2"));
        Assert.Equal("b", sql.Parameters.Get<string>(":3"));
        Assert.Equal(3, sql.Parameters.Get<int>(":4"));
        Assert.Equal("c", sql.Parameters.Get<string>(":5"));
        Assert.Equal(4, sql.Parameters.Get<int>(":6"));
        Assert.Equal("d", sql.Parameters.Get<string>(":7"));
        Assert.Equal(5, sql.Parameters.Get<int>(":8"));
        Assert.Equal("e", sql.Parameters.Get<string>(":9"));
        Assert.Equal(6, sql.Parameters.Get<int>(":10"));
        Assert.Equal("f", sql.Parameters.Get<string>(":11"));
        Assert.Equal(7, sql.Parameters.Get<int>(":12"));
        Assert.Equal("g", sql.Parameters.Get<string>(":13"));
        Assert.Equal(8, sql.Parameters.Get<int>(":14"));
        Assert.Equal("h", sql.Parameters.Get<string>(":15"));
        Assert.Equal("z", sql.Parameters.Get<string>(":16"));
    }

    [Fact]
    public void Decode_NinePairs_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    (1, "a"),
                    (2, "b"),
                    (3, "c"),
                    (4, "d"),
                    (5, "e"),
                    (6, "f"),
                    (7, "g"),
                    (8, "h"),
                    (9, "i"),
                    "z"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4, :5, :6, :7, :8, :9, :10, :11, :12, :13, :14, :15, :16, :17, :18)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal(2, sql.Parameters.Get<int>(":2"));
        Assert.Equal("b", sql.Parameters.Get<string>(":3"));
        Assert.Equal(3, sql.Parameters.Get<int>(":4"));
        Assert.Equal("c", sql.Parameters.Get<string>(":5"));
        Assert.Equal(4, sql.Parameters.Get<int>(":6"));
        Assert.Equal("d", sql.Parameters.Get<string>(":7"));
        Assert.Equal(5, sql.Parameters.Get<int>(":8"));
        Assert.Equal("e", sql.Parameters.Get<string>(":9"));
        Assert.Equal(6, sql.Parameters.Get<int>(":10"));
        Assert.Equal("f", sql.Parameters.Get<string>(":11"));
        Assert.Equal(7, sql.Parameters.Get<int>(":12"));
        Assert.Equal("g", sql.Parameters.Get<string>(":13"));
        Assert.Equal(8, sql.Parameters.Get<int>(":14"));
        Assert.Equal("h", sql.Parameters.Get<string>(":15"));
        Assert.Equal(9, sql.Parameters.Get<int>(":16"));
        Assert.Equal("i", sql.Parameters.Get<string>(":17"));
        Assert.Equal("z", sql.Parameters.Get<string>(":18"));
    }

    [Fact]
    public void Decode_TenPairs_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Decode(
                    _t.Code,
                    (1, "a"),
                    (2, "b"),
                    (3, "c"),
                    (4, "d"),
                    (5, "e"),
                    (6, "f"),
                    (7, "g"),
                    (8, "h"),
                    (9, "i"),
                    (10, "j"),
                    "z"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DECODE(\"t\".code, :0, :1, :2, :3, :4, :5, :6, :7, :8, :9, :10, :11, :12, :13, :14, :15, :16, :17, :18, :19, :20)");

        Assert.Equal(expected.ToString(), sql.Text);

        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("a", sql.Parameters.Get<string>(":1"));
        Assert.Equal(2, sql.Parameters.Get<int>(":2"));
        Assert.Equal("b", sql.Parameters.Get<string>(":3"));
        Assert.Equal(3, sql.Parameters.Get<int>(":4"));
        Assert.Equal("c", sql.Parameters.Get<string>(":5"));
        Assert.Equal(4, sql.Parameters.Get<int>(":6"));
        Assert.Equal("d", sql.Parameters.Get<string>(":7"));
        Assert.Equal(5, sql.Parameters.Get<int>(":8"));
        Assert.Equal("e", sql.Parameters.Get<string>(":9"));
        Assert.Equal(6, sql.Parameters.Get<int>(":10"));
        Assert.Equal("f", sql.Parameters.Get<string>(":11"));
        Assert.Equal(7, sql.Parameters.Get<int>(":12"));
        Assert.Equal("g", sql.Parameters.Get<string>(":13"));
        Assert.Equal(8, sql.Parameters.Get<int>(":14"));
        Assert.Equal("h", sql.Parameters.Get<string>(":15"));
        Assert.Equal(9, sql.Parameters.Get<int>(":16"));
        Assert.Equal("i", sql.Parameters.Get<string>(":17"));
        Assert.Equal(10, sql.Parameters.Get<int>(":18"));
        Assert.Equal("j", sql.Parameters.Get<string>(":19"));
        Assert.Equal("z", sql.Parameters.Get<string>(":20"));
    }
}
