using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>TO_CHAR(expr)</c> function: converts <paramref name="expr"/> to its
    /// default character representation.
    /// </summary>
    /// <param name="expr">The value to convert to text.</param>
    /// <returns>A <c>TO_CHAR</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static ToCharFunction ToChar(object expr) =>
        new(Resolve(expr));

    /// <inheritdoc cref="ToChar(object)"/>
    /// <param name="expr">The value to convert to text.</param>
    /// <param name="format">The Oracle format model controlling the output.</param>
    public static ToCharFunction ToChar(
        object expr,
        object format) => new(
            Resolve(expr),
            Resolve(format));

    /// <summary>
    /// The <c>TO_DATE(text, format)</c> function: parses <paramref name="text"/> into
    /// a date using the Oracle <paramref name="format"/> model.
    /// </summary>
    /// <param name="text">The string to parse.</param>
    /// <param name="format">The Oracle format model describing <paramref name="text"/>.</param>
    /// <returns>A <c>TO_DATE</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static ToDateFunction ToDate(
        object text,
        object format) => new(
            Resolve(text),
            Resolve(format));

    /// <summary>
    /// The <c>TO_NUMBER(expr)</c> function: converts <paramref name="expr"/> to a
    /// number.
    /// </summary>
    /// <param name="expr">The value to convert.</param>
    /// <returns>A <c>TO_NUMBER</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static ToNumberFunction ToNumber(object expr) =>
        new(Resolve(expr));

    /// <inheritdoc cref="ToNumber(object)"/>
    /// <param name="expr">The value to convert.</param>
    /// <param name="numericFormat">The Oracle numeric format model describing <paramref name="expr"/>.</param>
    public static ToNumberFunction ToNumber(
        object expr,
        object numericFormat) => new(
            Resolve(expr),
            Resolve(numericFormat));

    /// <summary>
    /// The <c>TO_TIMESTAMP(text, format)</c> function: parses <paramref name="text"/>
    /// into a timestamp using the Oracle <paramref name="format"/> model.
    /// </summary>
    /// <param name="text">The string to parse.</param>
    /// <param name="format">The Oracle format model describing <paramref name="text"/>.</param>
    /// <returns>A <c>TO_TIMESTAMP</c> function expression.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static ToTimestampFunction ToTimestamp(
        object text,
        object format) => new(
            Resolve(text),
            Resolve(format));

    /// <summary>
    /// The <c>TRIM(source)</c> function: removes leading and trailing spaces from
    /// <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to trim.</param>
    /// <returns>A <c>TRIM</c> function expression.</returns>
    public static TrimFunction Trim(object source) =>
        new(Resolve(source));

    /// <summary>
    /// The <c>TRIM(BOTH trimChar FROM source)</c> function: removes leading and
    /// trailing occurrences of <paramref name="trimChar"/> from
    /// <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to trim.</param>
    /// <param name="trimChar">The character to strip from both ends instead of spaces.</param>
    /// <returns>A <c>TRIM</c> function expression.</returns>
    public static TrimFunction Trim(
        object source,
        object trimChar) => new(
            Resolve(source),
            Resolve(trimChar));

    /// <summary>
    /// The <c>TRUNC(expr)</c> function: truncates <paramref name="expr"/> toward zero
    /// to an integer.
    /// </summary>
    /// <param name="expr">The numeric or date value to truncate.</param>
    /// <returns>A <c>TRUNC</c> function expression.</returns>
    /// <remarks>Oracle syntax. To truncate a timestamp to a date/time field on
    /// PostgreSQL use <see cref="DateTrunc(DateTimePart, object)"/>.</remarks>
    public static TruncFunction Trunc(object expr) =>
        new(Resolve(expr));

    /// <inheritdoc cref="Trunc(object)"/>
    /// <param name="expr">The numeric or date value to truncate.</param>
    /// <param name="format">The number of decimal places, or the Oracle date format unit (e.g. <c>'MM'</c>).</param>
    public static TruncFunction Trunc(
        object expr,
        object format) => new(
            Resolve(expr),
            Resolve(format));
}
