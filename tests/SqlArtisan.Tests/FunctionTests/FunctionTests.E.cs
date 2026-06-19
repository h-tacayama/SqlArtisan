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
                Extract(DateTimePart.Microsecond, CurrentTimestamp),
                Extract(DateTimePart.Second, CurrentTimestamp),
                Extract(DateTimePart.Minute, CurrentTimestamp),
                Extract(DateTimePart.Hour, CurrentTimestamp),
                Extract(DateTimePart.Day, CurrentTimestamp),
                Extract(DateTimePart.Week, CurrentTimestamp),
                Extract(DateTimePart.Month, CurrentTimestamp),
                Extract(DateTimePart.Quarter, CurrentTimestamp),
                Extract(DateTimePart.Year, CurrentTimestamp),
                Extract(DateTimePart.SecondMicrosecond, CurrentTimestamp),
                Extract(DateTimePart.MinuteMicrosecond, CurrentTimestamp),
                Extract(DateTimePart.MinuteSecond, CurrentTimestamp),
                Extract(DateTimePart.HourMicrosecond, CurrentTimestamp),
                Extract(DateTimePart.HourSecond, CurrentTimestamp),
                Extract(DateTimePart.HourMinute, CurrentTimestamp),
                Extract(DateTimePart.DayMicrosecond, CurrentTimestamp),
                Extract(DateTimePart.DaySecond, CurrentTimestamp),
                Extract(DateTimePart.DayMinute, CurrentTimestamp),
                Extract(DateTimePart.DayHour, CurrentTimestamp),
                Extract(DateTimePart.YearMonth, CurrentTimestamp)
            )
            .Build(Dbms.MySql);

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
                Extract(DateTimePart.Year, Systimestamp),
                Extract(DateTimePart.Month, Systimestamp),
                Extract(DateTimePart.Day, Systimestamp),
                Extract(DateTimePart.Hour, Systimestamp),
                Extract(DateTimePart.Minute, Systimestamp),
                Extract(DateTimePart.Second, Systimestamp),
                Extract(DateTimePart.TimezoneHour, Systimestamp),
                Extract(DateTimePart.TimezoneMinute, Systimestamp),
                Extract(DateTimePart.TimezoneRegion, Systimestamp),
                Extract(DateTimePart.TimezoneAbbr, Systimestamp))
            .From(Dual)
            .Build(Dbms.Oracle);

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
                Extract(DateTimePart.Century, CurrentTimestamp),
                Extract(DateTimePart.Day, CurrentTimestamp),
                Extract(DateTimePart.Decade, CurrentTimestamp),
                Extract(DateTimePart.Dow, CurrentTimestamp),
                Extract(DateTimePart.Doy, CurrentTimestamp),
                Extract(DateTimePart.Epoch, CurrentTimestamp),
                Extract(DateTimePart.Hour, CurrentTimestamp),
                Extract(DateTimePart.Isodow, CurrentTimestamp),
                Extract(DateTimePart.Isoyear, CurrentTimestamp),
                Extract(DateTimePart.Julian, CurrentTimestamp),
                Extract(DateTimePart.Microseconds, CurrentTimestamp),
                Extract(DateTimePart.Millennium, CurrentTimestamp),
                Extract(DateTimePart.Milliseconds, CurrentTimestamp),
                Extract(DateTimePart.Minute, CurrentTimestamp),
                Extract(DateTimePart.Month, CurrentTimestamp),
                Extract(DateTimePart.Quarter, CurrentTimestamp),
                Extract(DateTimePart.Second, CurrentTimestamp),
                Extract(DateTimePart.Timezone, CurrentTimestamp),
                Extract(DateTimePart.TimezoneHour, CurrentTimestamp),
                Extract(DateTimePart.TimezoneMinute, CurrentTimestamp),
                Extract(DateTimePart.Week, CurrentTimestamp),
                Extract(DateTimePart.Year, CurrentTimestamp)
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
