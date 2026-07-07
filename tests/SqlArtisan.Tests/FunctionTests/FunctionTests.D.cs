using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Dateadd_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(Dateadd(DateTimePart.Month, 3, _t.CreatedAt))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DATEADD(MONTH, @0, \"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(3, sql.Parameters.Get<int>("@0"));
    }

    [Fact]
    public void Dateadd_SqlServer_NegativeNumberSubtracts()
    {
        SqlStatement sql =
            Select(Dateadd(DateTimePart.Day, -7, _t.CreatedAt))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DATEADD(DAY, @0, \"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(-7, sql.Parameters.Get<int>("@0"));
    }

    [Fact]
    public void Datediff_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(Datediff(DateTimePart.Day, _t.CreatedAt, CurrentTimestamp))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DATEDIFF(DAY, \"t\".created_at, CURRENT_TIMESTAMP)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DateFormat_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(DateFormat(_t.CreatedAt, "%Y-%m"))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DATE_FORMAT(`t`.created_at, ?0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("%Y-%m", sql.Parameters.Get<string>("?0"));
    }

    [Fact]
    public void Datepart_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Datepart(DateTimePart.Year, CurrentTimestamp),
                Datepart(DateTimePart.Quarter, CurrentTimestamp),
                Datepart(DateTimePart.Month, CurrentTimestamp),
                Datepart(DateTimePart.Dayofyear, CurrentTimestamp),
                Datepart(DateTimePart.Day, CurrentTimestamp),
                Datepart(DateTimePart.Week, CurrentTimestamp),
                Datepart(DateTimePart.Weekday, CurrentTimestamp),
                Datepart(DateTimePart.Hour, CurrentTimestamp),
                Datepart(DateTimePart.Minute, CurrentTimestamp),
                Datepart(DateTimePart.Second, CurrentTimestamp),
                Datepart(DateTimePart.Millisecond, CurrentTimestamp),
                Datepart(DateTimePart.Microsecond, CurrentTimestamp),
                Datepart(DateTimePart.Nanosecond, CurrentTimestamp),
                Datepart(DateTimePart.Tzoffset, CurrentTimestamp),
                Datepart(DateTimePart.IsoWeek, CurrentTimestamp))
            .Build(Dbms.SqlServer);

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
    public void DateTrunc_PostgreSql_CorrectSql()
    {
        SqlStatement sql =
            Select(DateTrunc(DateTimePart.Month, _t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DATE_TRUNC('MONTH', \"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Datetrunc_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(Datetrunc(DateTimePart.Month, _t.CreatedAt))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DATETRUNC(MONTH, \"t\".created_at)");

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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
            .Build(Dbms.Oracle);

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

    [Fact]
    public void DoublePipe_TwoValues_CorrectSql()
    {
        SqlStatement sql =
            Select(DoublePipe(_t.Name, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("(\"t\".name || :0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void DoublePipe_MultipleValues_CorrectSql()
    {
        SqlStatement sql =
            Select(DoublePipe(_t.Name, "a", "b"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("(\"t\".name || :0 || :1)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("b", sql.Parameters.Get<string>(":1"));
    }

    [Fact]
    public void DoublePipe_MySql_StillEmitsDoublePipe()
    {
        // DoublePipe(...) emits || verbatim on every dialect, including MySQL,
        // where it is logical OR under the default sql_mode — the SQL you write
        // is the SQL that runs, and the analyzer flags this trap instead.
        SqlStatement sql =
            Select(DoublePipe(_t.Name, "a"))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("(`t`.name || ?0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
