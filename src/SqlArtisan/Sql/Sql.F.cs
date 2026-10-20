using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>FIRST_VALUE(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the first row of the window frame.
    /// </summary>
    /// <param name="expr">The expression to read from the first row of the frame.</param>
    /// <returns>A window function emitting
    /// <c>FIRST_VALUE(expr)</c>.</returns>
    public static ValueAnalyticFunction FirstValue(object expr) =>
        new AnalyticFirstValueFunction(Resolve(expr));

    /// <summary>
    /// The <c>FLOOR(expr)</c> function (largest integer not greater than
    /// <paramref name="expr"/>).
    /// </summary>
    /// <param name="expr">The numeric expression to round down.</param>
    /// <returns>An expression emitting <c>FLOOR(expr)</c>.</returns>
    /// <remarks>MySQL, Oracle, PostgreSQL, SQLite (3.35+), and SQL Server syntax.</remarks>
    public static SqlExpression Floor(object expr) =>
        new FloorFunction(Resolve(expr));

    /// <summary>
    /// A <c>n FOLLOWING</c> window-frame bound (offset rows/range after the
    /// current row).
    /// </summary>
    /// <param name="offset">The number of rows/range units after the current row; non-negative.</param>
    /// <returns>A <see cref="FrameBound"/> for the <c>n FOLLOWING</c> bound.</returns>
    public static FrameBound Following(int offset) => FrameBound.Following(offset);

    /// <summary>
    /// The <c>FORMAT(<paramref name="value"/>, <paramref name="format"/>)</c>
    /// function: <paramref name="value"/> (a number or date/time) rendered as a
    /// string per the .NET-style <paramref name="format"/> string.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The .NET-style format string (e.g. <c>"yyyy-MM-dd"</c>).</param>
    /// <returns>The <c>FORMAT</c> function expression.</returns>
    /// <remarks>
    /// SQL Server syntax. Three other dialects have a same-named but incompatible
    /// <c>FORMAT()</c>: MySQL's formats a number to a fixed decimal count
    /// (<c>FORMAT(number, decimals[, locale])</c>), and PostgreSQL's
    /// <c>format(text, ...)</c> and SQLite's (3.38+) are <c>printf()</c>-style
    /// substitution. PostgreSQL errors unless the first argument is text; where
    /// the call does run, it does not carry these semantics.
    /// </remarks>
    public static SqlExpression Format(object value, object format) =>
        new FormatFunction(Resolve(value), Resolve(format));

    /// <inheritdoc cref="Format(object, object)"/>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The .NET-style format string (e.g. <c>"yyyy-MM-dd"</c>).</param>
    /// <param name="culture">The culture name (e.g. <c>"en-US"</c>) the format is
    /// interpreted under.</param>
    public static SqlExpression Format(object value, object format, object culture) =>
        new FormatFunction(Resolve(value), Resolve(format), Resolve(culture));

    /// <summary>
    /// The SQL Server full-text <c>FREETEXT(column, freetext)</c> predicate:
    /// matches rows whose <paramref name="column"/> matches the meaning — not the
    /// exact wording — of <paramref name="freetext"/>. Requires a full-text index
    /// on the column.
    /// </summary>
    /// <param name="column">The full-text indexed column to search.</param>
    /// <param name="freetext">The free-form text to match by meaning.</param>
    /// <returns>A condition emitting
    /// <c>FREETEXT(column, freetext)</c>.</returns>
    /// <remarks>SQL Server syntax. For exact words, prefixes, and boolean
    /// combinations use <see cref="Contains(object, object)"/>.</remarks>
    public static SqlCondition Freetext(object column, object freetext) =>
        new FreetextCondition(Resolve(column), Resolve(freetext));
}
