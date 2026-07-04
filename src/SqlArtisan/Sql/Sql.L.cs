using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>LAG(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row one position before the current row
    /// in the window.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <returns>An <see cref="AnalyticLagFunction"/> emitting <c>LAG(expr)</c>.</returns>
    public static AnalyticLagFunction Lag(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LAG(expr, offset)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// before the current row.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look back from the current row.</param>
    /// <returns>An <see cref="AnalyticLagFunction"/> emitting <c>LAG(expr, offset)</c>.</returns>
    /// <remarks>The offset is emitted as an integer literal.</remarks>
    public static AnalyticLagFunction Lag(object expr, int offset) =>
        new(Resolve(expr), offset);

    /// <summary>
    /// The <c>LAG(expr, offset, default)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// before the current row, or <paramref name="defaultValue"/> when that row
    /// falls outside the partition.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look back from the current row.</param>
    /// <param name="defaultValue">The value returned when the offset row falls outside the partition.</param>
    /// <returns>An <see cref="AnalyticLagFunction"/> emitting <c>LAG(expr, offset, default)</c>.</returns>
    /// <remarks>The offset is emitted as an integer literal; the default value is
    /// parameterized.</remarks>
    public static AnalyticLagFunction Lag(object expr, int offset, object defaultValue) =>
        new(Resolve(expr), offset, Resolve(defaultValue));

    /// <summary>
    /// The <c>LAST_DAY(<paramref name="date"/>)</c> function: the date
    /// of the last day of the month containing <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The date whose month's last day is returned.</param>
    /// <returns>The LAST_DAY construct.</returns>
    /// <remarks>MySQL and Oracle syntax.</remarks>
    public static LastDayFunction LastDay(object date) =>
        new(Resolve(date));

    /// <summary>
    /// The <c>LAST_VALUE(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the last row of the window frame.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <returns>An <see cref="AnalyticLastValueFunction"/> emitting <c>LAST_VALUE(expr)</c>.</returns>
    /// <remarks>The default frame ends at the current row, so an explicit frame
    /// (e.g. <c>ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING</c>) is
    /// usually intended.</remarks>
    public static AnalyticLastValueFunction LastValue(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LEAD(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row one position after the current row
    /// in the window.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <returns>An <see cref="AnalyticLeadFunction"/> emitting <c>LEAD(expr)</c>.</returns>
    public static AnalyticLeadFunction Lead(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LEAD(expr, offset)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// after the current row.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look ahead from the current row.</param>
    /// <returns>An <see cref="AnalyticLeadFunction"/> emitting <c>LEAD(expr, offset)</c>.</returns>
    /// <remarks>The offset is emitted as an integer literal.</remarks>
    public static AnalyticLeadFunction Lead(object expr, int offset) =>
        new(Resolve(expr), offset);

    /// <summary>
    /// The <c>LEAD(expr, offset, default)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// after the current row, or <paramref name="defaultValue"/> when that row
    /// falls outside the partition.
    /// </summary>
    /// <param name="expr">The value evaluated for each row of the window.</param>
    /// <param name="offset">The number of rows to look ahead from the current row.</param>
    /// <param name="defaultValue">The value returned when the offset row falls outside the partition.</param>
    /// <returns>An <see cref="AnalyticLeadFunction"/> emitting <c>LEAD(expr, offset, default)</c>.</returns>
    /// <remarks>The offset is emitted as an integer literal; the default value is
    /// parameterized.</remarks>
    public static AnalyticLeadFunction Lead(object expr, int offset, object defaultValue) =>
        new(Resolve(expr), offset, Resolve(defaultValue));

    /// <summary>
    /// The <c>LEAST(a, b, ...)</c> function: the smallest of its
    /// <paramref name="expressions"/>.
    /// </summary>
    /// <param name="expressions">The values to compare.</param>
    /// <returns>The LEAST construct.</returns>
    public static LeastFunction Least(params object[] expressions) =>
        new(Resolve(expressions));

    /// <summary>
    /// The <c>LENGTH(<paramref name="source"/>)</c> function: the number of
    /// characters in <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string whose length is measured.</param>
    /// <returns>The LENGTH construct.</returns>
    public static LengthFunction Length(object source) =>
        new(Resolve(source));

    /// <summary>
    /// The <c>LENGTHB(<paramref name="source"/>)</c> function: the
    /// length of <paramref name="source"/> in bytes.
    /// </summary>
    /// <param name="source">The string whose byte length is measured.</param>
    /// <returns>The LENGTHB construct.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static LengthbFunction Lengthb(object source) =>
        new(Resolve(source));

    /// <summary>
    /// The <c>LISTAGG(expr, separator)</c> string aggregate. Complete
    /// it with <c>.WithinGroup(OrderBy(...))</c> to supply Oracle's mandatory
    /// ordering.
    /// </summary>
    /// <param name="expr">The value aggregated into the concatenated string.</param>
    /// <param name="separator">The separator placed between values.</param>
    /// <returns>A <see cref="ListaggFunction"/> emitting
    /// <c>LISTAGG(expr, separator)</c>.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static ListaggFunction Listagg(object expr, object separator) =>
        new(Resolve(expr), Resolve(separator));

    /// <summary>
    /// The <c>LOWER(<paramref name="source"/>)</c> function: lowercases
    /// <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to lowercase.</param>
    /// <returns>The LOWER construct.</returns>
    public static LowerFunction Lower(object source) =>
        new(Resolve(source));

    /// <summary>
    /// The <c>LPAD(<paramref name="source"/>, <paramref name="length"/>)</c>
    /// function: left-pads <paramref name="source"/> with spaces to
    /// <paramref name="length"/> characters (truncating if longer).
    /// </summary>
    /// <param name="source">The string to pad.</param>
    /// <param name="length">The target total length.</param>
    /// <returns>The LPAD construct.</returns>
    public static LpadFunction Lpad(object source, object length) =>
        new(Resolve(source), Resolve(length));

    /// <inheritdoc cref="Lpad(object, object)"/>
    /// <param name="source">The string to pad.</param>
    /// <param name="length">The target total length.</param>
    /// <param name="padding">The string to pad with instead of spaces.</param>
    public static LpadFunction Lpad(object source, object length, object padding) =>
        new(Resolve(source), Resolve(length), Resolve(padding));

    /// <summary>
    /// The <c>LTRIM(<paramref name="source"/>)</c> function: removes leading
    /// whitespace from <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The string to trim.</param>
    /// <returns>The LTRIM construct.</returns>
    public static LtrimFunction Ltrim(object source) =>
        new(Resolve(source));

    /// <inheritdoc cref="Ltrim(object)"/>
    /// <param name="source">The string to trim.</param>
    /// <param name="trimChars">The set of characters to strip from the left.</param>
    public static LtrimFunction Ltrim(object source, object trimChars) =>
        new(Resolve(source), Resolve(trimChars));
}
