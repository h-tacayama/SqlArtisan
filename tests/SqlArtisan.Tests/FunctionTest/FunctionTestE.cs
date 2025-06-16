using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTest
{
    [Fact]
    public void Extract_MySQL_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Extract(DatePart.Microsecond, CurrentTimestamp),
                Extract(DatePart.Second, CurrentTimestamp),
                Extract(DatePart.Minute, CurrentTimestamp),
                Extract(DatePart.Hour, CurrentTimestamp),
                Extract(DatePart.Day, CurrentTimestamp),
                Extract(DatePart.Week, CurrentTimestamp),
                Extract(DatePart.Month, CurrentTimestamp),
                Extract(DatePart.Quarter, CurrentTimestamp),
                Extract(DatePart.Year, CurrentTimestamp),
                Extract(DatePart.SecondMicrosecond, CurrentTimestamp),
                Extract(DatePart.MinuteMicrosecond, CurrentTimestamp),
                Extract(DatePart.MinuteSecond, CurrentTimestamp),
                Extract(DatePart.HourMicrosecond, CurrentTimestamp),
                Extract(DatePart.HourSecond, CurrentTimestamp),
                Extract(DatePart.HourMinute, CurrentTimestamp),
                Extract(DatePart.DayMicrosecond, CurrentTimestamp),
                Extract(DatePart.DaySecond, CurrentTimestamp),
                Extract(DatePart.DayMinute, CurrentTimestamp),
                Extract(DatePart.DayHour, CurrentTimestamp),
                Extract(DatePart.YearMonth, CurrentTimestamp)
            )
            .Build();

        StringBuilder expected = new StringBuilder()
            .Append("SELECT ")
            .Append("EXTRACT(MICROSECOND FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(SECOND FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(MINUTE FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(HOUR FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(DAY FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(WEEK FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(MONTH FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(QUARTER FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(YEAR FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(SECOND_MICROSECOND FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(MINUTE_MICROSECOND FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(MINUTE_SECOND FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(HOUR_MICROSECOND FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(HOUR_SECOND FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(HOUR_MINUTE FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(DAY_MICROSECOND FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(DAY_SECOND FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(DAY_MINUTE FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(DAY_HOUR FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(YEAR_MONTH FROM CURRENT_TIMESTAMP)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Extract_Oracle_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Extract(DatePart.Year, SysTimestamp),
                Extract(DatePart.Month, SysTimestamp),
                Extract(DatePart.Day, SysTimestamp),
                Extract(DatePart.Hour, SysTimestamp),
                Extract(DatePart.Minute, SysTimestamp),
                Extract(DatePart.Second, SysTimestamp),
                Extract(DatePart.TimezoneHour, SysTimestamp),
                Extract(DatePart.TimezoneMinute, SysTimestamp),
                Extract(DatePart.TimezoneRegion, SysTimestamp),
                Extract(DatePart.TimezoneAbbr, SysTimestamp))
            .From(Dual)
            .Build();

        StringBuilder expected = new StringBuilder()
            .Append("SELECT ")
            .Append("EXTRACT(YEAR FROM SYSTIMESTAMP), ")
            .Append("EXTRACT(MONTH FROM SYSTIMESTAMP), ")
            .Append("EXTRACT(DAY FROM SYSTIMESTAMP), ")
            .Append("EXTRACT(HOUR FROM SYSTIMESTAMP), ")
            .Append("EXTRACT(MINUTE FROM SYSTIMESTAMP), ")
            .Append("EXTRACT(SECOND FROM SYSTIMESTAMP), ")
            .Append("EXTRACT(TIMEZONE_HOUR FROM SYSTIMESTAMP), ")
            .Append("EXTRACT(TIMEZONE_MINUTE FROM SYSTIMESTAMP), ")
            .Append("EXTRACT(TIMEZONE_REGION FROM SYSTIMESTAMP), ")
            .Append("EXTRACT(TIMEZONE_ABBR FROM SYSTIMESTAMP) ")
            .Append("FROM DUAL");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Extract_PostgreSQL_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Extract(DatePart.Century, CurrentTimestamp),
                Extract(DatePart.Day, CurrentTimestamp),
                Extract(DatePart.Decade, CurrentTimestamp),
                Extract(DatePart.Dow, CurrentTimestamp),
                Extract(DatePart.Doy, CurrentTimestamp),
                Extract(DatePart.Epoch, CurrentTimestamp),
                Extract(DatePart.Hour, CurrentTimestamp),
                Extract(DatePart.Isodow, CurrentTimestamp),
                Extract(DatePart.Isoyear, CurrentTimestamp),
                Extract(DatePart.Julian, CurrentTimestamp),
                Extract(DatePart.Microseconds, CurrentTimestamp),
                Extract(DatePart.Millennium, CurrentTimestamp),
                Extract(DatePart.Milliseconds, CurrentTimestamp),
                Extract(DatePart.Minute, CurrentTimestamp),
                Extract(DatePart.Month, CurrentTimestamp),
                Extract(DatePart.Quarter, CurrentTimestamp),
                Extract(DatePart.Second, CurrentTimestamp),
                Extract(DatePart.Timezone, CurrentTimestamp),
                Extract(DatePart.TimezoneHour, CurrentTimestamp),
                Extract(DatePart.TimezoneMinute, CurrentTimestamp),
                Extract(DatePart.Week, CurrentTimestamp),
                Extract(DatePart.Year, CurrentTimestamp)
            )
            .Build();

        StringBuilder expected = new StringBuilder()
            .Append("SELECT ")
            .Append("EXTRACT(CENTURY FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(DAY FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(DECADE FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(DOW FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(DOY FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(EPOCH FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(HOUR FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(ISODOW FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(ISOYEAR FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(JULIAN FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(MICROSECONDS FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(MILLENNIUM FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(MILLISECONDS FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(MINUTE FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(MONTH FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(QUARTER FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(SECOND FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(TIMEZONE FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(TIMEZONE_HOUR FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(TIMEZONE_MINUTE FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(WEEK FROM CURRENT_TIMESTAMP), ")
            .Append("EXTRACT(YEAR FROM CURRENT_TIMESTAMP)");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
