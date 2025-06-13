namespace SqlArtisan.Internal;

internal static class DatePartExtensions
{
    internal static string ToSql(this DatePart datePart) => datePart switch
    {
        DatePart.Century => "CENTURY",
        DatePart.Day => "DAY",
        DatePart.DayHour => "DAY_HOUR",
        DatePart.DayMicrosecond => "DAY_MICROSECOND",
        DatePart.DayMinute => "DAY_MINUTE",
        DatePart.DaySecond => "DAY_SECOND",
        DatePart.Decade => "DECADE",
        DatePart.Dow => "DOW",
        DatePart.Doy => "DOY",
        DatePart.Epoch => "EPOCH",
        DatePart.Hour => "HOUR",
        DatePart.HourMicrosecond => "HOUR_MICROSECOND",
        DatePart.HourMinute => "HOUR_MINUTE",
        DatePart.HourSecond => "HOUR_SECOND",
        DatePart.Isodow => "ISODOW",
        DatePart.Isoyear => "ISOYEAR",
        DatePart.Microsecond => "MICROSECOND",
        DatePart.Microseconds => "MICROSECONDS",
        DatePart.Millennium => "MILLENNIUM",
        DatePart.Milliseconds => "MILLISECONDS",
        DatePart.Minute => "MINUTE",
        DatePart.MinuteMicrosecond => "MINUTE_MICROSECOND",
        DatePart.MinuteSecond => "MINUTE_SECOND",
        DatePart.Month => "MONTH",
        DatePart.Quarter => "QUARTER",
        DatePart.Second => "SECOND",
        DatePart.SecondMicrosecond => "SECOND_MICROSECOND",
        DatePart.Timezone => "TIMEZONE",
        DatePart.TimezoneAbbr => "TIMEZONE_ABBR",
        DatePart.TimezoneHour => "TIMEZONE_HOUR",
        DatePart.TimezoneMinute => "TIMEZONE_MINUTE",
        DatePart.TimezoneRegion => "TIMEZONE_REGION",
        DatePart.Week => "WEEK",
        DatePart.Year => "YEAR",
        DatePart.YearMonth => "YEAR_MONTH",
        _ => throw new ArgumentOutOfRangeException(nameof(datePart), datePart, null)
    };
}
