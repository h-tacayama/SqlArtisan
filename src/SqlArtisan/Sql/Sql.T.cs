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
    /// The PostgreSQL <c>TO_TSQUERY(text)</c> function: parses <paramref name="text"/>
    /// already in tsquery syntax (operators <c>&amp;</c>, <c>|</c>, <c>!</c>) into a
    /// text-search query. Pair with a tsvector via
    /// <see cref="TsMatch(object, object)"/>.
    /// </summary>
    /// <param name="text">The tsquery-syntax text to parse.</param>
    /// <returns>A <see cref="ToTsqueryFunction"/> emitting <c>TO_TSQUERY(text)</c>.</returns>
    /// <remarks>PostgreSQL syntax. For plain, unformatted text use
    /// <see cref="PlaintoTsquery(object)"/>.</remarks>
    public static ToTsqueryFunction ToTsquery(object text) =>
        new(null, Resolve(text));

    /// <inheritdoc cref="ToTsquery(object)"/>
    /// <param name="config">The text-search configuration (e.g. <c>"english"</c>),
    /// emitted as an inline string literal.</param>
    /// <param name="text">The tsquery-syntax text to parse.</param>
    public static ToTsqueryFunction ToTsquery(
        string config,
        object text) => new(
            config,
            Resolve(text));

    /// <summary>
    /// The PostgreSQL <c>TO_TSVECTOR(document)</c> function: reduces
    /// <paramref name="document"/> to a text-search vector. Pair with a tsquery via
    /// <see cref="TsMatch(object, object)"/>.
    /// </summary>
    /// <param name="document">The document expression to reduce.</param>
    /// <returns>A <see cref="ToTsvectorFunction"/> emitting
    /// <c>TO_TSVECTOR(document)</c>.</returns>
    /// <remarks>PostgreSQL syntax.</remarks>
    public static ToTsvectorFunction ToTsvector(object document) =>
        new(null, Resolve(document));

    /// <inheritdoc cref="ToTsvector(object)"/>
    /// <param name="config">The text-search configuration (e.g. <c>"english"</c>),
    /// emitted as an inline string literal.</param>
    /// <param name="document">The document expression to reduce.</param>
    public static ToTsvectorFunction ToTsvector(
        string config,
        object document) => new(
            config,
            Resolve(document));

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

    /// <summary>
    /// The PostgreSQL text-search match predicate <c>vector @@ query</c>: whether
    /// <paramref name="vector"/> (a tsvector, e.g. <see cref="ToTsvector(object)"/>)
    /// matches <paramref name="query"/> (a tsquery, e.g.
    /// <see cref="ToTsquery(object)"/> or <see cref="PlaintoTsquery(object)"/>).
    /// </summary>
    /// <param name="vector">The tsvector expression to search.</param>
    /// <param name="query">The tsquery expression to match.</param>
    /// <returns>A <see cref="TsMatchCondition"/> emitting <c>vector @@ query</c>.</returns>
    /// <remarks>PostgreSQL syntax.</remarks>
    public static TsMatchCondition TsMatch(
        object vector,
        object query) => new(
            Resolve(vector),
            Resolve(query));
}
