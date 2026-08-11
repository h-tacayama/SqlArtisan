using System;
using System.Collections.Generic;

namespace SqlArtisan.Analyzers;

/// <summary>
/// The per-(member, dialect) set of <c>DateTimePart</c> member names each
/// vendor grammar accepts for SQLA0104. Seven primary-source-verified lists
/// (WebSearch, since docs.oracle.com / postgresql.org / dev.mysql.com /
/// learn.microsoft.com direct fetch is blocked in this environment) cover the
/// eleven (member, dialect) pairs, since MySQL's <c>EXTRACT</c>/<c>INTERVAL</c>
/// share one unit list, SQL Server's <c>DATEPART</c>/<c>DATEADD</c>/
/// <c>DATEDIFF</c> share one datepart list, and MySQL's
/// <c>TIMESTAMPADD</c>/<c>TIMESTAMPDIFF</c> share their own (simple-units-only)
/// list.
/// </summary>
internal static class DatepartValidity
{
    // MySQL EXTRACT() takes the same unit specifiers as DATE_ADD()/DATE_SUB(),
    // which Sql.Interval's `unit` also feeds (dev.mysql.com Date and Time Functions).
    private static readonly HashSet<string> MySqlTemporalUnits = new(StringComparer.Ordinal)
    {
        "Microsecond", "Second", "Minute", "Hour", "Day", "Week", "Month", "Quarter", "Year",
        "SecondMicrosecond", "MinuteMicrosecond", "MinuteSecond", "HourMicrosecond", "HourSecond",
        "HourMinute", "DayMicrosecond", "DaySecond", "DayMinute", "DayHour", "YearMonth",
    };

    // dev.mysql.com Date and Time Functions, TIMESTAMPADD/TIMESTAMPDIFF: the nine simple
    // units only — unlike MySqlTemporalUnits above, these two reject the compound
    // DAY_HOUR-style units (EXTRACT/INTERVAL/DATE_ADD/DATE_SUB's grammar, not theirs).
    private static readonly HashSet<string> MySqlTimestampUnits = new(StringComparer.Ordinal)
    {
        "Microsecond", "Second", "Minute", "Hour", "Day", "Week", "Month", "Quarter", "Year",
    };

    // docs.oracle.com EXTRACT (datetime): YEAR/MONTH/DAY require a DATE-family
    // source; HOUR/MINUTE/SECOND require TIMESTAMP; the four TIMEZONE_* fields
    // require TIMESTAMP WITH TIME ZONE — a source-type constraint this table
    // does not model (see docs/analyzer.md's known-limitations note).
    private static readonly HashSet<string> OracleExtractFields = new(StringComparer.Ordinal)
    {
        "Year", "Month", "Day", "Hour", "Minute", "Second",
        "TimezoneHour", "TimezoneMinute", "TimezoneRegion", "TimezoneAbbr",
    };

    // postgresql.org Date/Time Functions and Operators, EXTRACT.
    private static readonly HashSet<string> PostgreSqlExtractFields = new(StringComparer.Ordinal)
    {
        "Century", "Day", "Decade", "Dow", "Doy", "Epoch", "Hour", "Isodow", "Isoyear", "Julian",
        "Microseconds", "Millennium", "Milliseconds", "Minute", "Month", "Quarter", "Second",
        "Timezone", "TimezoneHour", "TimezoneMinute", "Week", "Year",
    };

    // postgresql.org Date/Time Functions and Operators, date_trunc — Epoch, Dow,
    // Doy, Isodow, Isoyear, Julian, and the three Timezone* fields are EXTRACT-only.
    private static readonly HashSet<string> PostgreSqlDateTruncFields = new(StringComparer.Ordinal)
    {
        "Microseconds", "Milliseconds", "Second", "Minute", "Hour", "Day", "Week", "Month",
        "Quarter", "Year", "Decade", "Century", "Millennium",
    };

    // learn.microsoft.com DATEPART/DATEADD/DATEDIFF, which share one datepart
    // table (each also accepts an abbreviation of these names, e.g. "yy" for
    // Year — the analyzer only ever sees a DateTimePart member, never a raw
    // string, so abbreviations are out of scope).
    private static readonly HashSet<string> SqlServerDatepartFields = new(StringComparer.Ordinal)
    {
        "Year", "Quarter", "Month", "Dayofyear", "Day", "Week", "Weekday", "Hour", "Minute",
        "Second", "Millisecond", "Microsecond", "Nanosecond", "Tzoffset", "IsoWeek",
    };

    // learn.microsoft.com DATETRUNC: every SqlServerDatepartFields member except
    // Weekday/Tzoffset/Nanosecond, which that page states are not supported.
    // Microsecond support is further data-type-dependent (datetime2 rejects it);
    // that constraint is not modeled (see docs/analyzer.md's known limitations).
    private static readonly HashSet<string> SqlServerDateTruncFields = new(StringComparer.Ordinal)
    {
        "Year", "Quarter", "Month", "Dayofyear", "Day", "Week", "IsoWeek", "Hour", "Minute",
        "Second", "Millisecond", "Microsecond",
    };

    private static readonly Dictionary<(string Member, TargetDbms Dbms), HashSet<string>> Table = new()
    {
        [("Extract", TargetDbms.MySql)] = MySqlTemporalUnits,
        [("Extract", TargetDbms.Oracle)] = OracleExtractFields,
        [("Extract", TargetDbms.PostgreSql)] = PostgreSqlExtractFields,
        [("Datepart", TargetDbms.SqlServer)] = SqlServerDatepartFields,
        [("Dateadd", TargetDbms.SqlServer)] = SqlServerDatepartFields,
        [("Datediff", TargetDbms.SqlServer)] = SqlServerDatepartFields,
        [("DateTrunc", TargetDbms.PostgreSql)] = PostgreSqlDateTruncFields,
        [("Datetrunc", TargetDbms.SqlServer)] = SqlServerDateTruncFields,
        [("Interval", TargetDbms.MySql)] = MySqlTemporalUnits,
        [("Timestampadd", TargetDbms.MySql)] = MySqlTimestampUnits,
        [("Timestampdiff", TargetDbms.MySql)] = MySqlTimestampUnits,
    };

    // The parameter SQLA0104 reads the literal DateTimePart out of — "unit" for
    // Interval, "datepart" for the other six (matches each factory's own
    // parameter name in Sql.*.cs).
    internal static readonly Dictionary<string, string> DatepartParameterName = new(StringComparer.Ordinal)
    {
        ["Extract"] = "datepart",
        ["Datepart"] = "datepart",
        ["Dateadd"] = "datepart",
        ["Datediff"] = "datepart",
        ["DateTrunc"] = "datepart",
        ["Datetrunc"] = "datepart",
        ["Interval"] = "unit",
        ["Timestampadd"] = "unit",
        ["Timestampdiff"] = "unit",
    };

    /// <summary>
    /// The valid <c>DateTimePart</c> member-name set for <paramref name="memberName"/>
    /// on <paramref name="dbms"/>, or <see langword="null"/> when this rule has no
    /// list for that pair — nothing to check, stay silent.
    /// </summary>
    public static HashSet<string>? For(string memberName, TargetDbms dbms) =>
        Table.TryGetValue((memberName, dbms), out HashSet<string>? set) ? set : null;

    /// <summary>Every member name appearing in at least one list — the parity
    /// gate's coverage check.</summary>
    internal static IEnumerable<string> AllKnownMemberNames
    {
        get
        {
            HashSet<string> seen = new(StringComparer.Ordinal);

            foreach (HashSet<string> set in Table.Values)
            {
                seen.UnionWith(set);
            }

            return seen;
        }
    }
}
