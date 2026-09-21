using System;
using System.Collections.Generic;

namespace SqlArtisan.Analyzers;

/// <summary>
/// The per-(member, dialect) set of <c>DateTimePart</c> member names each
/// vendor grammar accepts for SQLA0104; the lists are vendor-documentation
/// facts, shared where the vendor shares them.
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

    // Oracle EXTRACT's per-field source-type constraint is not modeled
    // (docs/analyzer.md, known limitations).
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

    // date_trunc takes the EXTRACT fields that name a truncation boundary.
    private static readonly HashSet<string> PostgreSqlDateTruncFields = new(StringComparer.Ordinal)
    {
        "Microseconds", "Milliseconds", "Second", "Minute", "Hour", "Day", "Week", "Month",
        "Quarter", "Year", "Decade", "Century", "Millennium",
    };

    // learn.microsoft.com DATEPART (each entry also accepts an abbreviation,
    // e.g. "yy" for Year — the analyzer only ever sees a DateTimePart member,
    // never a raw string, so abbreviations are out of scope).
    private static readonly HashSet<string> SqlServerDatepartFields = new(StringComparer.Ordinal)
    {
        "Year", "Quarter", "Month", "Dayofyear", "Day", "Week", "Weekday", "Hour", "Minute",
        "Second", "Millisecond", "Microsecond", "Nanosecond", "Tzoffset", "IsoWeek",
    };

    // learn.microsoft.com DATEADD and DATEDIFF, whose shared datepart table
    // stops at Nanosecond — neither accepts DATEPART's Tzoffset or IsoWeek.
    private static readonly HashSet<string> SqlServerDateaddFields = new(StringComparer.Ordinal)
    {
        "Year", "Quarter", "Month", "Dayofyear", "Day", "Week", "Weekday", "Hour", "Minute",
        "Second", "Millisecond", "Microsecond", "Nanosecond",
    };

    // DATETRUNC's data-type-dependent Microsecond support is not modeled
    // (docs/analyzer.md, known limitations).
    private static readonly HashSet<string> SqlServerDateTruncFields = new(StringComparer.Ordinal)
    {
        "Year", "Quarter", "Month", "Dayofyear", "Day", "Week", "IsoWeek", "Hour", "Minute",
        "Second", "Millisecond", "Microsecond",
    };

    private static readonly Dictionary<(string Member, TargetDbms Dbms), HashSet<string>> Table =
        new()
        {
            [("Extract", TargetDbms.MySql)] = MySqlTemporalUnits,
            [("Extract", TargetDbms.Oracle)] = OracleExtractFields,
            [("Extract", TargetDbms.PostgreSql)] = PostgreSqlExtractFields,
            [("Datepart", TargetDbms.SqlServer)] = SqlServerDatepartFields,
            [("Dateadd", TargetDbms.SqlServer)] = SqlServerDateaddFields,
            [("Datediff", TargetDbms.SqlServer)] = SqlServerDateaddFields,
            [("DateTrunc", TargetDbms.PostgreSql)] = PostgreSqlDateTruncFields,
            [("Datetrunc", TargetDbms.SqlServer)] = SqlServerDateTruncFields,
            [("Interval", TargetDbms.MySql)] = MySqlTemporalUnits,
            [("Timestampadd", TargetDbms.MySql)] = MySqlTimestampUnits,
            [("Timestampdiff", TargetDbms.MySql)] = MySqlTimestampUnits,
        };

    // The parameter SQLA0104 reads the literal DateTimePart out of — each entry
    // matches that factory's own parameter name in Sql.*.cs.
    internal static readonly Dictionary<string, string> DatepartParameterName = new(
        StringComparer.Ordinal)
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

    /// <summary>Every <c>DateTimePart</c> name appearing in at least one list —
    /// the parity gate's coverage check.</summary>
    internal static IEnumerable<string> AllKnownDatepartNames
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
