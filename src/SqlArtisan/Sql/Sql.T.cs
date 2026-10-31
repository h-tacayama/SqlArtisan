using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>TIMESTAMPADD(<paramref name="unit"/>, <paramref name="number"/>,
    /// <paramref name="dateTime"/>)</c> function: <paramref name="dateTime"/> shifted by
    /// <paramref name="number"/> units of <paramref name="unit"/>. Pass a negative
    /// <paramref name="number"/> to subtract.
    /// </summary>
    /// <param name="unit">The unit to add (year, month, day, ...).</param>
    /// <param name="number">The number of units to add; negative to subtract.</param>
    /// <param name="dateTime">The date/time value to shift.</param>
    /// <returns>The <c>TIMESTAMPADD</c> function expression.</returns>
    /// <remarks>
    /// MySQL syntax — independent of, and does not require,
    /// <see cref="Interval(object, DateTimePart)"/>; the unit is a bare keyword,
    /// not an <c>INTERVAL</c> expression.
    /// </remarks>
    public static SqlExpression Timestampadd(
        DateTimePart unit,
        object number,
        object dateTime) =>
        new TimestampaddFunction(
            unit,
            Resolve(number, nameof(number)),
            Resolve(dateTime, nameof(dateTime)));

    /// <summary>
    /// The <c>TIMESTAMPDIFF(<paramref name="unit"/>, <paramref name="startDate"/>,
    /// <paramref name="endDate"/>)</c> function: <paramref name="endDate"/> minus
    /// <paramref name="startDate"/>, counted in whole units of <paramref name="unit"/>.
    /// </summary>
    /// <param name="unit">The unit to count the difference in.</param>
    /// <param name="startDate">The start date/time.</param>
    /// <param name="endDate">The end date/time.</param>
    /// <returns>The <c>TIMESTAMPDIFF</c> function expression.</returns>
    /// <remarks>
    /// MySQL syntax. Argument order is <paramref name="startDate"/> then
    /// <paramref name="endDate"/> — the reverse of MySQL's own two-argument
    /// <c>DATEDIFF(expr1, expr2)</c> (<c>expr1 - expr2</c>).
    /// </remarks>
    public static SqlExpression Timestampdiff(
        DateTimePart unit,
        object startDate,
        object endDate) => new TimestampdiffFunction(
            unit,
            Resolve(startDate, nameof(startDate)),
            Resolve(endDate, nameof(endDate)));

    /// <summary>
    /// The <c>TO_CHAR(expr)</c> function: converts <paramref name="expr"/> to its
    /// default character representation.
    /// </summary>
    /// <param name="expr">The value to convert to text.</param>
    /// <returns>A <c>TO_CHAR</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static SqlExpression ToChar(object expr) =>
        new ToCharFunction(Resolve(expr, nameof(expr)));

    /// <inheritdoc cref="ToChar(object)"/>
    /// <param name="expr">The value to convert to text.</param>
    /// <param name="format">The format model (Oracle-style) controlling the output.</param>
    /// <remarks>Oracle and PostgreSQL syntax. PostgreSQL's <c>to_char</c> requires
    /// the format argument, so only this two-argument form runs there.</remarks>
    public static SqlExpression ToChar(object expr, object format) =>
        new ToCharFunction(Resolve(expr, nameof(expr)), Resolve(format, nameof(format)));

    /// <summary>
    /// The <c>TO_DATE(text, format)</c> function: parses <paramref name="text"/> into
    /// a date using the Oracle <paramref name="format"/> model.
    /// </summary>
    /// <param name="text">The string to parse.</param>
    /// <param name="format">The format model (Oracle-style) describing <paramref name="text"/>.</param>
    /// <returns>A <c>TO_DATE</c> function expression.</returns>
    /// <remarks>Oracle and PostgreSQL syntax.</remarks>
    public static SqlExpression ToDate(object text, object format) =>
        new ToDateFunction(Resolve(text, nameof(text)), Resolve(format, nameof(format)));

    /// <summary>
    /// The <c>HOUR</c> trailing interval field, for the trailing argument of
    /// <see cref="IntervalLiteral(string, IntervalField, IntervalField)"/>
    /// (e.g. <c>DAY TO HOUR</c>).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static IntervalField ToHour => new(DateTimePart.Hour, null);

    /// <summary>
    /// The <c>MINUTE</c> trailing interval field, for the trailing argument of
    /// <see cref="IntervalLiteral(string, IntervalField, IntervalField)"/>
    /// (e.g. <c>DAY TO MINUTE</c>, <c>HOUR TO MINUTE</c>).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static IntervalField ToMinute => new(DateTimePart.Minute, null);

    /// <summary>
    /// The <c>MONTH</c> trailing interval field, for the trailing argument of
    /// <see cref="IntervalLiteral(string, IntervalField, IntervalField)"/>
    /// (<c>YEAR TO MONTH</c>).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static IntervalField ToMonth => new(DateTimePart.Month, null);

    /// <summary>
    /// The <c>TO_NUMBER(expr)</c> function: converts <paramref name="expr"/> to a
    /// number.
    /// </summary>
    /// <param name="expr">The value to convert.</param>
    /// <returns>A <c>TO_NUMBER</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static SqlExpression ToNumber(object expr) =>
        new ToNumberFunction(Resolve(expr, nameof(expr)));

    /// <inheritdoc cref="ToNumber(object)"/>
    /// <param name="expr">The value to convert.</param>
    /// <param name="numericFormat">The numeric format model (Oracle-style) describing <paramref name="expr"/>.</param>
    /// <remarks>Oracle and PostgreSQL syntax. PostgreSQL's <c>to_number</c> requires
    /// the format argument, so only this two-argument form runs there.</remarks>
    public static SqlExpression ToNumber(object expr, object numericFormat) =>
        new ToNumberFunction(
            Resolve(expr, nameof(expr)),
            Resolve(numericFormat, nameof(numericFormat)));

    /// <summary>
    /// SQL Server's <c>TOP (n)</c> select prefix, <c>Select(Top(n), ...)</c>: the first
    /// n rows. Chain <c>.WithTies()</c> (ties under the <c>ORDER BY</c> ride along) or
    /// <c>.Percent()</c>; not combinable with <c>LIMIT</c>, <c>OFFSET</c>, or <c>FETCH</c>.
    /// </summary>
    /// <param name="count">The row count (or percent, with <c>.Percent()</c>).</param>
    /// <returns>A <see cref="TopClause"/> for a <c>SELECT</c> prefix.</returns>
    /// <remarks>SQL Server syntax.</remarks>
    public static TopClause Top(int count) => new(count);

    /// <summary>
    /// The <c>SECOND</c> trailing interval field, for the trailing argument of
    /// <see cref="IntervalLiteral(string, IntervalField, IntervalField)"/>
    /// (e.g. <c>DAY TO SECOND</c>).
    /// </summary>
    /// <returns>An <see cref="IntervalField"/> emitting <c>SECOND</c>.</returns>
    public static IntervalField ToSecond() => new(DateTimePart.Second, null);

    /// <inheritdoc cref="ToSecond()"/>
    /// <param name="precision">The fractional-second digit count, zero or more; the no-argument
    /// overload leaves Oracle's own default of 6. As a sole field rather than a range's trailing
    /// one, Oracle reads the same digits as the leading precision instead.</param>
    /// <returns>An <see cref="IntervalField"/> emitting <c>SECOND(precision)</c>.</returns>
    /// <remarks>Oracle and PostgreSQL syntax.</remarks>
    public static IntervalField ToSecond(int precision) =>
        new(DateTimePart.Second, precision);

    /// <summary>
    /// The <c>TO_TIMESTAMP(text, format)</c> function: parses <paramref name="text"/>
    /// into a timestamp using the Oracle <paramref name="format"/> model.
    /// </summary>
    /// <param name="text">The string to parse.</param>
    /// <param name="format">The format model (Oracle-style) describing <paramref name="text"/>.</param>
    /// <returns>A <c>TO_TIMESTAMP</c> function expression.</returns>
    /// <remarks>Oracle and PostgreSQL syntax.</remarks>
    public static SqlExpression ToTimestamp(object text, object format) =>
        new ToTimestampFunction(Resolve(text, nameof(text)), Resolve(format, nameof(format)));

    /// <summary>
    /// The PostgreSQL <c>TO_TSQUERY(text)</c> function: parses <paramref name="text"/>
    /// already in tsquery syntax (operators <c>&amp;</c>, <c>|</c>, <c>!</c>) into a
    /// text-search query. Pair with a tsvector via
    /// <see cref="TsMatch(object, object)"/>.
    /// </summary>
    /// <param name="text">The tsquery-syntax text to parse.</param>
    /// <returns>An expression emitting <c>TO_TSQUERY(text)</c>.</returns>
    /// <remarks>PostgreSQL syntax. For plain, unformatted text use
    /// <see cref="PlaintoTsquery(object)"/>.</remarks>
    public static SqlExpression ToTsquery(object text) =>
        new ToTsqueryFunction(null, Resolve(text, nameof(text)));

    /// <inheritdoc cref="ToTsquery(object)"/>
    /// <param name="config">The text-search configuration (e.g. <c>"english"</c>),
    /// emitted as an inline string literal.</param>
    /// <param name="text">The tsquery-syntax text to parse.</param>
    public static SqlExpression ToTsquery(string config, object text)
    {
        StringGuard.ThrowIfNullOrEmpty(config, "TO_TSQUERY requires a configuration name.");
        return new ToTsqueryFunction(config, Resolve(text, nameof(text)));
    }

    /// <summary>
    /// The PostgreSQL <c>TO_TSVECTOR(document)</c> function: reduces
    /// <paramref name="document"/> to a text-search vector. Pair with a tsquery via
    /// <see cref="TsMatch(object, object)"/>.
    /// </summary>
    /// <param name="document">The document expression to reduce.</param>
    /// <returns>An expression emitting
    /// <c>TO_TSVECTOR(document)</c>.</returns>
    /// <remarks>PostgreSQL syntax.</remarks>
    public static SqlExpression ToTsvector(object document) =>
        new ToTsvectorFunction(null, Resolve(document, nameof(document)));

    /// <inheritdoc cref="ToTsvector(object)"/>
    /// <param name="config">The text-search configuration (e.g. <c>"english"</c>),
    /// emitted as an inline string literal.</param>
    /// <param name="document">The document expression to reduce.</param>
    public static SqlExpression ToTsvector(string config, object document)
    {
        StringGuard.ThrowIfNullOrEmpty(config, "TO_TSVECTOR requires a configuration name.");
        return new ToTsvectorFunction(config, Resolve(document, nameof(document)));
    }

    /// <summary>
    /// The <c>TRIM(source)</c> function: removes leading and trailing spaces from
    /// <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to trim.</param>
    /// <returns>A <c>TRIM</c> function expression.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL, SQLite, and SQL Server (2017+) syntax.</remarks>
    public static SqlExpression Trim(object source) =>
        new TrimFunction(Resolve(source, nameof(source)));

    /// <summary>
    /// The <c>TRIM(trimChar FROM source)</c> function: removes leading and
    /// trailing occurrences of <paramref name="trimChar"/> from
    /// <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to trim.</param>
    /// <param name="trimChar">The character to strip from both ends instead of spaces.</param>
    /// <returns>A <c>TRIM</c> function expression.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL, and SQL Server (2017+) syntax. Not supported by
    /// SQLite, whose positional <c>trim(source, trimChar)</c> is a separate function this does
    /// not emit.</remarks>
    public static SqlExpression Trim(object source, object trimChar) =>
        new TrimFunction(Resolve(source, nameof(source)), Resolve(trimChar, nameof(trimChar)));

    /// <summary>
    /// The <c>TRUNC(expr)</c> function: truncates <paramref name="expr"/> — a number
    /// toward zero to an integer, or a date to the start of its day.
    /// </summary>
    /// <param name="expr">The numeric or date value to truncate.</param>
    /// <returns>A <c>TRUNC</c> function expression.</returns>
    /// <remarks>Oracle, PostgreSQL, and SQLite (3.35+) syntax, SQLite with one argument. Only
    /// Oracle truncates a date: PostgreSQL 16 rejects one, and SQLite 3.50 gives NULL for a date
    /// string; on PostgreSQL, see <see cref="DateTrunc(DateTimePart, object)"/>.</remarks>
    public static SqlExpression Trunc(object expr) =>
        new TruncFunction(Resolve(expr, nameof(expr)));

    /// <inheritdoc cref="Trunc(object)"/>
    /// <param name="expr">The numeric or date value to truncate.</param>
    /// <param name="format">The number of decimal places, or the Oracle date format unit (e.g. <c>'MM'</c>).</param>
    public static SqlExpression Trunc(object expr, object format) =>
        new TruncFunction(Resolve(expr, nameof(expr)), Resolve(format, nameof(format)));

    /// <summary>
    /// The PostgreSQL text-search match predicate <c>vector @@ query</c>: whether
    /// <paramref name="vector"/> (a tsvector, e.g. <see cref="ToTsvector(object)"/>)
    /// matches <paramref name="query"/> (a tsquery, e.g.
    /// <see cref="ToTsquery(object)"/> or <see cref="PlaintoTsquery(object)"/>).
    /// </summary>
    /// <param name="vector">The tsvector expression to search.</param>
    /// <param name="query">The tsquery expression to match.</param>
    /// <returns>A condition emitting <c>vector @@ query</c>.</returns>
    /// <remarks>PostgreSQL syntax.</remarks>
    public static SqlCondition TsMatch(object vector, object query) =>
        new TsMatchCondition(
            ResolveCompared(vector, nameof(vector), Operators.TsMatch),
            ResolveQuantifiable(query, nameof(query), Operators.TsMatch));
}
