using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Extract_MySQL_CorrectSql()
    {
        SqlStatement sql =
            Select(
                Extract(DateTimeField.Microsecond, CurrentTimestamp),
                Extract(DateTimeField.Second, CurrentTimestamp),
                Extract(DateTimeField.Minute, CurrentTimestamp),
                Extract(DateTimeField.Hour, CurrentTimestamp),
                Extract(DateTimeField.Day, CurrentTimestamp),
                Extract(DateTimeField.Week, CurrentTimestamp),
                Extract(DateTimeField.Month, CurrentTimestamp),
                Extract(DateTimeField.Quarter, CurrentTimestamp),
                Extract(DateTimeField.Year, CurrentTimestamp),
                Extract(DateTimeField.SecondMicrosecond, CurrentTimestamp),
                Extract(DateTimeField.MinuteMicrosecond, CurrentTimestamp),
                Extract(DateTimeField.MinuteSecond, CurrentTimestamp),
                Extract(DateTimeField.HourMicrosecond, CurrentTimestamp),
                Extract(DateTimeField.HourSecond, CurrentTimestamp),
                Extract(DateTimeField.HourMinute, CurrentTimestamp),
                Extract(DateTimeField.DayMicrosecond, CurrentTimestamp),
                Extract(DateTimeField.DaySecond, CurrentTimestamp),
                Extract(DateTimeField.DayMinute, CurrentTimestamp),
                Extract(DateTimeField.DayHour, CurrentTimestamp),
                Extract(DateTimeField.YearMonth, CurrentTimestamp)
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
                Extract(DateTimeField.Year, Systimestamp),
                Extract(DateTimeField.Month, Systimestamp),
                Extract(DateTimeField.Day, Systimestamp),
                Extract(DateTimeField.Hour, Systimestamp),
                Extract(DateTimeField.Minute, Systimestamp),
                Extract(DateTimeField.Second, Systimestamp),
                Extract(DateTimeField.TimezoneHour, Systimestamp),
                Extract(DateTimeField.TimezoneMinute, Systimestamp),
                Extract(DateTimeField.TimezoneRegion, Systimestamp),
                Extract(DateTimeField.TimezoneAbbr, Systimestamp))
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
                Extract(DateTimeField.Century, CurrentTimestamp),
                Extract(DateTimeField.Day, CurrentTimestamp),
                Extract(DateTimeField.Decade, CurrentTimestamp),
                Extract(DateTimeField.Dow, CurrentTimestamp),
                Extract(DateTimeField.Doy, CurrentTimestamp),
                Extract(DateTimeField.Epoch, CurrentTimestamp),
                Extract(DateTimeField.Hour, CurrentTimestamp),
                Extract(DateTimeField.Isodow, CurrentTimestamp),
                Extract(DateTimeField.Isoyear, CurrentTimestamp),
                Extract(DateTimeField.Julian, CurrentTimestamp),
                Extract(DateTimeField.Microseconds, CurrentTimestamp),
                Extract(DateTimeField.Millennium, CurrentTimestamp),
                Extract(DateTimeField.Milliseconds, CurrentTimestamp),
                Extract(DateTimeField.Minute, CurrentTimestamp),
                Extract(DateTimeField.Month, CurrentTimestamp),
                Extract(DateTimeField.Quarter, CurrentTimestamp),
                Extract(DateTimeField.Second, CurrentTimestamp),
                Extract(DateTimeField.Timezone, CurrentTimestamp),
                Extract(DateTimeField.TimezoneHour, CurrentTimestamp),
                Extract(DateTimeField.TimezoneMinute, CurrentTimestamp),
                Extract(DateTimeField.Week, CurrentTimestamp),
                Extract(DateTimeField.Year, CurrentTimestamp)
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
