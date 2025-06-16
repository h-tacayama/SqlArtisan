using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void DatePart_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(
                DatePart(DatePart.Year, CurrentTimestamp),
                DatePart(DatePart.Quarter, CurrentTimestamp),
                DatePart(DatePart.Month, CurrentTimestamp),
                DatePart(DatePart.Dayofyear, CurrentTimestamp),
                DatePart(DatePart.Day, CurrentTimestamp),
                DatePart(DatePart.Week, CurrentTimestamp),
                DatePart(DatePart.Weekday, CurrentTimestamp),
                DatePart(DatePart.Hour, CurrentTimestamp),
                DatePart(DatePart.Minute, CurrentTimestamp),
                DatePart(DatePart.Second, CurrentTimestamp),
                DatePart(DatePart.Millisecond, CurrentTimestamp),
                DatePart(DatePart.Microsecond, CurrentTimestamp),
                DatePart(DatePart.Nanosecond, CurrentTimestamp),
                DatePart(DatePart.Tzoffset, CurrentTimestamp),
                DatePart(DatePart.IsoWeek, CurrentTimestamp))
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
    }
}
