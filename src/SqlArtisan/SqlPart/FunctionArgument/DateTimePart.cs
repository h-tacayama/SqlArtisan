namespace SqlArtisan;

/// <summary>
/// A date/time field to extract or operate on. This is a superset shared across
/// <see cref="Sql.Extract(DateTimePart, object)"/>,
/// <see cref="Sql.Datepart(DateTimePart, object)"/>,
/// <see cref="Sql.Dateadd(DateTimePart, object, object)"/>,
/// <see cref="Sql.Datediff(DateTimePart, object, object)"/>, and
/// <see cref="Sql.DateTrunc(DateTimePart, object)"/>; not every field is valid for
/// every function or dialect.
/// </summary>
public enum DateTimePart
{
    /// <summary>
    /// The <c>CENTURY</c> field.
    /// </summary>
    Century = 0,

    /// <summary>
    /// The <c>DAY</c> field — day of the month.
    /// </summary>
    Day = 1,

    /// <summary>
    /// The <c>DAY_HOUR</c> compound unit (MySQL), spanning days through hours.
    /// </summary>
    DayHour = 2,

    /// <summary>
    /// The <c>DAY_MICROSECOND</c> compound unit (MySQL), spanning days through microseconds.
    /// </summary>
    DayMicrosecond = 3,

    /// <summary>
    /// The <c>DAY_MINUTE</c> compound unit (MySQL), spanning days through minutes.
    /// </summary>
    DayMinute = 4,

    /// <summary>
    /// The <c>DAYOFYEAR</c> field — day of the year (SQL Server / MySQL spelling; PostgreSQL uses <see cref="Doy"/>).
    /// </summary>
    Dayofyear = 5,

    /// <summary>
    /// The <c>DAY_SECOND</c> compound unit (MySQL), spanning days through seconds.
    /// </summary>
    DaySecond = 6,

    /// <summary>
    /// The <c>DECADE</c> field — the year divided by 10.
    /// </summary>
    Decade = 7,

    /// <summary>
    /// The <c>DOW</c> field — day of the week (PostgreSQL: Sunday = 0).
    /// </summary>
    Dow = 8,

    /// <summary>
    /// The <c>DOY</c> field — day of the year (PostgreSQL).
    /// </summary>
    Doy = 9,

    /// <summary>
    /// The <c>EPOCH</c> field — seconds since 1970-01-01 00:00:00 UTC (PostgreSQL).
    /// </summary>
    Epoch = 10,

    /// <summary>
    /// The <c>HOUR</c> field — hour of the day.
    /// </summary>
    Hour = 11,

    /// <summary>
    /// The <c>HOUR_MICROSECOND</c> compound unit (MySQL), spanning hours through microseconds.
    /// </summary>
    HourMicrosecond = 12,

    /// <summary>
    /// The <c>HOUR_MINUTE</c> compound unit (MySQL), spanning hours through minutes.
    /// </summary>
    HourMinute = 13,

    /// <summary>
    /// The <c>HOUR_SECOND</c> compound unit (MySQL), spanning hours through seconds.
    /// </summary>
    HourSecond = 14,

    /// <summary>
    /// The <c>ISO_WEEK</c> field — ISO 8601 week number (week 1 holds the year's first Thursday).
    /// </summary>
    IsoWeek = 15,

    /// <summary>
    /// The <c>ISODOW</c> field — ISO 8601 day of the week (Monday = 1 … Sunday = 7).
    /// </summary>
    Isodow = 16,

    /// <summary>
    /// The <c>ISOYEAR</c> field — ISO 8601 week-numbering year.
    /// </summary>
    Isoyear = 17,

    /// <summary>
    /// The <c>JULIAN</c> field — Julian day number.
    /// </summary>
    Julian = 18,

    /// <summary>
    /// The <c>MICROSECOND</c> field — the microseconds component.
    /// </summary>
    Microsecond = 19,

    /// <summary>
    /// The <c>MICROSECONDS</c> field — the seconds field including fractional microseconds (PostgreSQL).
    /// </summary>
    Microseconds = 20,

    /// <summary>
    /// The <c>MILLENNIUM</c> field.
    /// </summary>
    Millennium = 21,

    /// <summary>
    /// The <c>MILLISECOND</c> field — the milliseconds component.
    /// </summary>
    Millisecond = 22,

    /// <summary>
    /// The <c>MILLISECONDS</c> field — the seconds field including fractional milliseconds (PostgreSQL).
    /// </summary>
    Milliseconds = 23,

    /// <summary>
    /// The <c>MINUTE</c> field — minute of the hour.
    /// </summary>
    Minute = 24,

    /// <summary>
    /// The <c>MINUTE_MICROSECOND</c> compound unit (MySQL), spanning minutes through microseconds.
    /// </summary>
    MinuteMicrosecond = 25,

    /// <summary>
    /// The <c>MINUTE_SECOND</c> compound unit (MySQL), spanning minutes through seconds.
    /// </summary>
    MinuteSecond = 26,

    /// <summary>
    /// The <c>MONTH</c> field — month of the year.
    /// </summary>
    Month = 27,

    /// <summary>
    /// The <c>NANOSECOND</c> field — the nanoseconds component.
    /// </summary>
    Nanosecond = 28,

    /// <summary>
    /// The <c>QUARTER</c> field — calendar quarter (1–4).
    /// </summary>
    Quarter = 29,

    /// <summary>
    /// The <c>SECOND</c> field — second of the minute.
    /// </summary>
    Second = 30,

    /// <summary>
    /// The <c>SECOND_MICROSECOND</c> compound unit (MySQL), spanning seconds through microseconds.
    /// </summary>
    SecondMicrosecond = 31,

    /// <summary>
    /// The <c>TIMEZONE</c> field — the UTC offset in seconds (PostgreSQL).
    /// </summary>
    Timezone = 32,

    /// <summary>
    /// The <c>TIMEZONE_ABBR</c> field — the time-zone abbreviation.
    /// </summary>
    TimezoneAbbr = 33,

    /// <summary>
    /// The <c>TIMEZONE_HOUR</c> field — the hours component of the UTC offset.
    /// </summary>
    TimezoneHour = 34,

    /// <summary>
    /// The <c>TIMEZONE_MINUTE</c> field — the minutes component of the UTC offset.
    /// </summary>
    TimezoneMinute = 35,

    /// <summary>
    /// The <c>TIMEZONE_REGION</c> field — the time-zone region name (Oracle).
    /// </summary>
    TimezoneRegion = 36,

    /// <summary>
    /// The <c>TZOFFSET</c> field — the UTC offset in minutes (SQL Server).
    /// </summary>
    Tzoffset = 37,

    /// <summary>
    /// The <c>WEEK</c> field — week of the year.
    /// </summary>
    Week = 38,

    /// <summary>
    /// The <c>WEEKDAY</c> field — day of the week (SQL Server / MySQL).
    /// </summary>
    Weekday = 39,

    /// <summary>
    /// The <c>YEAR</c> field.
    /// </summary>
    Year = 40,

    /// <summary>
    /// The <c>YEAR_MONTH</c> compound unit (MySQL), spanning years and months.
    /// </summary>
    YearMonth = 41,
}
