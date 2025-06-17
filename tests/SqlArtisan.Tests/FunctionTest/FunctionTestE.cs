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
                Extract(Datepart.Microsecond, CurrentTimestamp),
                Extract(Datepart.Second, CurrentTimestamp),
                Extract(Datepart.Minute, CurrentTimestamp),
                Extract(Datepart.Hour, CurrentTimestamp),
                Extract(Datepart.Day, CurrentTimestamp),
                Extract(Datepart.Week, CurrentTimestamp),
                Extract(Datepart.Month, CurrentTimestamp),
                Extract(Datepart.Quarter, CurrentTimestamp),
                Extract(Datepart.Year, CurrentTimestamp),
                Extract(Datepart.SecondMicrosecond, CurrentTimestamp),
                Extract(Datepart.MinuteMicrosecond, CurrentTimestamp),
                Extract(Datepart.MinuteSecond, CurrentTimestamp),
                Extract(Datepart.HourMicrosecond, CurrentTimestamp),
                Extract(Datepart.HourSecond, CurrentTimestamp),
                Extract(Datepart.HourMinute, CurrentTimestamp),
                Extract(Datepart.DayMicrosecond, CurrentTimestamp),
                Extract(Datepart.DaySecond, CurrentTimestamp),
                Extract(Datepart.DayMinute, CurrentTimestamp),
                Extract(Datepart.DayHour, CurrentTimestamp),
                Extract(Datepart.YearMonth, CurrentTimestamp)
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
                Extract(Datepart.Year, Systimestamp),
                Extract(Datepart.Month, Systimestamp),
                Extract(Datepart.Day, Systimestamp),
                Extract(Datepart.Hour, Systimestamp),
                Extract(Datepart.Minute, Systimestamp),
                Extract(Datepart.Second, Systimestamp),
                Extract(Datepart.TimezoneHour, Systimestamp),
                Extract(Datepart.TimezoneMinute, Systimestamp),
                Extract(Datepart.TimezoneRegion, Systimestamp),
                Extract(Datepart.TimezoneAbbr, Systimestamp))
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
                Extract(Datepart.Century, CurrentTimestamp),
                Extract(Datepart.Day, CurrentTimestamp),
                Extract(Datepart.Decade, CurrentTimestamp),
                Extract(Datepart.Dow, CurrentTimestamp),
                Extract(Datepart.Doy, CurrentTimestamp),
                Extract(Datepart.Epoch, CurrentTimestamp),
                Extract(Datepart.Hour, CurrentTimestamp),
                Extract(Datepart.Isodow, CurrentTimestamp),
                Extract(Datepart.Isoyear, CurrentTimestamp),
                Extract(Datepart.Julian, CurrentTimestamp),
                Extract(Datepart.Microseconds, CurrentTimestamp),
                Extract(Datepart.Millennium, CurrentTimestamp),
                Extract(Datepart.Milliseconds, CurrentTimestamp),
                Extract(Datepart.Minute, CurrentTimestamp),
                Extract(Datepart.Month, CurrentTimestamp),
                Extract(Datepart.Quarter, CurrentTimestamp),
                Extract(Datepart.Second, CurrentTimestamp),
                Extract(Datepart.Timezone, CurrentTimestamp),
                Extract(Datepart.TimezoneHour, CurrentTimestamp),
                Extract(Datepart.TimezoneMinute, CurrentTimestamp),
                Extract(Datepart.Week, CurrentTimestamp),
                Extract(Datepart.Year, CurrentTimestamp)
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
