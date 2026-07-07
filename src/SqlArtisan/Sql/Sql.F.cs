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
    /// <returns>An <see cref="AnalyticFirstValueFunction"/> emitting
    /// <c>FIRST_VALUE(expr)</c>.</returns>
    public static AnalyticFirstValueFunction FirstValue(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>FLOOR(expr)</c> function (largest integer not greater than
    /// <paramref name="expr"/>).
    /// </summary>
    /// <param name="expr">The numeric expression to round down.</param>
    /// <returns>A <see cref="FloorFunction"/> emitting <c>FLOOR(expr)</c>.</returns>
    public static FloorFunction Floor(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// A <c>n FOLLOWING</c> window-frame bound (offset rows/range after the
    /// current row).
    /// </summary>
    /// <param name="offset">The number of rows/range units after the current row.</param>
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
    /// SQL Server syntax. Two other dialects have a same-named but incompatible
    /// <c>FORMAT()</c>, so the call executes there without erroring but not with
    /// these semantics: MySQL's formats a number to a fixed decimal count
    /// (<c>FORMAT(number, decimals[, locale])</c>), and SQLite's (3.38+) is a
    /// <c>printf()</c> alias using <c>%s</c>/<c>%d</c>-style substitution.
    /// </remarks>
    public static FormatFunction Format(object value, object format) =>
        new(Resolve(value), Resolve(format));

    /// <inheritdoc cref="Format(object, object)"/>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The .NET-style format string (e.g. <c>"yyyy-MM-dd"</c>).</param>
    /// <param name="culture">The culture name (e.g. <c>"en-US"</c>) the format is
    /// interpreted under.</param>
    public static FormatFunction Format(object value, object format, object culture) =>
        new(Resolve(value), Resolve(format), Resolve(culture));

    /// <summary>
    /// The SQL Server full-text <c>FREETEXT(column, freetext)</c> predicate:
    /// matches rows whose <paramref name="column"/> matches the meaning — not the
    /// exact wording — of <paramref name="freetext"/>. Requires a full-text index
    /// on the column.
    /// </summary>
    /// <param name="column">The full-text indexed column to search.</param>
    /// <param name="freetext">The free-form text to match by meaning.</param>
    /// <returns>A <see cref="FreetextCondition"/> emitting
    /// <c>FREETEXT(column, freetext)</c>.</returns>
    /// <remarks>SQL Server syntax. For exact words, prefixes, and boolean
    /// combinations use <see cref="Contains(object, object)"/>.</remarks>
    public static FreetextCondition Freetext(object column, object freetext) =>
        new(Resolve(column), Resolve(freetext));
}
