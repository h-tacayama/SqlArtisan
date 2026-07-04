using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>DATEADD(<paramref name="datepart"/>, <paramref name="number"/>, <paramref name="dateTime"/>)</c>
    /// function: <paramref name="dateTime"/> shifted by
    /// <paramref name="number"/> units of <paramref name="datepart"/>. Pass a
    /// negative <paramref name="number"/> to subtract.
    /// </summary>
    /// <param name="datepart">The unit to add (year, month, day, ...).</param>
    /// <param name="number">The number of units to add; negative to subtract.</param>
    /// <param name="dateTime">The date/time value to shift.</param>
    /// <returns>The <c>DATEADD</c> function expression.</returns>
    /// <remarks>
    /// This is SQL Server's form. For Oracle use
    /// <see cref="AddMonths(object, object)"/>; PostgreSQL/MySQL use interval
    /// arithmetic native to those dialects. <see cref="DateTimePart"/> is a superset
    /// shared with EXTRACT/DATEPART; only the dateparts SQL Server's <c>DATEADD</c>
    /// accepts are valid here.
    /// </remarks>
    public static DateaddFunction Dateadd(DateTimePart datepart, object number, object dateTime) =>
        new(datepart, Resolve(number), Resolve(dateTime));

    /// <summary>
    /// The <c>DATEDIFF(<paramref name="datepart"/>, <paramref name="startDate"/>, <paramref name="endDate"/>)</c>
    /// function: the number of <paramref name="datepart"/> boundaries
    /// crossed between <paramref name="startDate"/> and <paramref name="endDate"/>.
    /// </summary>
    /// <param name="datepart">The unit of the boundaries to count.</param>
    /// <param name="startDate">The start date/time.</param>
    /// <param name="endDate">The end date/time.</param>
    /// <returns>The <c>DATEDIFF</c> function expression.</returns>
    /// <remarks>
    /// Argument order and supported units are vendor-specific; this is the SQL
    /// Server form. <see cref="DateTimePart"/> is a superset shared with
    /// EXTRACT/DATEPART; only the dateparts SQL Server's <c>DATEDIFF</c> accepts
    /// are valid here.
    /// </remarks>
    public static DatediffFunction Datediff(
        DateTimePart datepart,
        object startDate,
        object endDate) => new(
            datepart,
            Resolve(startDate),
            Resolve(endDate));

    /// <summary>
    /// The <c>DATEPART(<paramref name="datepart"/>, <paramref name="source"/>)</c>
    /// function returning a single date/time field as an integer.
    /// </summary>
    /// <param name="datepart">The field of <paramref name="source"/> to return.</param>
    /// <param name="source">The date/time value to read the field from.</param>
    /// <returns>The <c>DATEPART</c> function expression.</returns>
    /// <remarks>
    /// This is SQL Server's form. For PostgreSQL and the SQL standard use
    /// <see cref="Extract(DateTimePart, object)"/>.
    /// </remarks>
    public static DatepartFunction Datepart(DateTimePart datepart, object source) =>
        new(datepart, Resolve(source));

    /// <summary>
    /// The <c>DATE_TRUNC('<paramref name="datepart"/>', <paramref name="source"/>)</c>
    /// function: <paramref name="source"/> truncated down to
    /// <paramref name="datepart"/> precision.
    /// </summary>
    /// <param name="datepart">The precision to truncate to.</param>
    /// <param name="source">The date/time value to truncate.</param>
    /// <returns>The <c>DATE_TRUNC</c> function expression.</returns>
    /// <remarks>
    /// This is PostgreSQL's form. For Oracle use the date/time overload of
    /// <see cref="Trunc(object, object)"/> (<c>TRUNC(date, fmt)</c>).
    /// <see cref="DateTimePart"/> is a superset shared with EXTRACT/DATEPART; only
    /// the fields PostgreSQL's <c>date_trunc</c> accepts are valid here.
    /// </remarks>
    public static DateTruncFunction DateTrunc(DateTimePart datepart, object source) =>
        new(datepart, Resolve(source));

    /// <summary>
    /// The <c>DECODE(<paramref name="expr"/>, search, result, ..., <paramref name="default"/>)</c>
    /// function: compares <paramref name="expr"/> against each search value and
    /// returns the matching result, or <paramref name="default"/> when none match.
    /// </summary>
    /// <param name="expr">The value to compare against each search.</param>
    /// <param name="searchResultPairs">The <c>(search, result)</c> pairs tested in order.</param>
    /// <param name="default">The result when no search matches (the <c>default</c> keyword parameter).</param>
    /// <returns>The <c>DECODE</c> function expression.</returns>
    /// <remarks>This is Oracle's form.</remarks>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result)[] searchResultPairs,
        object @default) => new(
            Resolve(expr),
            Resolve(searchResultPairs),
            Resolve(@default));

    /// <inheritdoc cref="Decode(object, System.ValueTuple{object, object}[], object)"/>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] { searchResultPair }),
            Resolve(@default));

    /// <inheritdoc cref="Decode(object, System.ValueTuple{object, object}[], object)"/>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,}),
            Resolve(@default));

    /// <inheritdoc cref="Decode(object, System.ValueTuple{object, object}[], object)"/>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,}),
            Resolve(@default));

    /// <inheritdoc cref="Decode(object, System.ValueTuple{object, object}[], object)"/>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,}),
            Resolve(@default));

    /// <inheritdoc cref="Decode(object, System.ValueTuple{object, object}[], object)"/>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,}),
            Resolve(@default));

    /// <inheritdoc cref="Decode(object, System.ValueTuple{object, object}[], object)"/>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        (object search, object result) searchResultPair6,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,
                searchResultPair6,}),
            Resolve(@default));

    /// <inheritdoc cref="Decode(object, System.ValueTuple{object, object}[], object)"/>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        (object search, object result) searchResultPair6,
        (object search, object result) searchResultPair7,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,
                searchResultPair6,
                searchResultPair7,}),
            Resolve(@default));

    /// <inheritdoc cref="Decode(object, System.ValueTuple{object, object}[], object)"/>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        (object search, object result) searchResultPair6,
        (object search, object result) searchResultPair7,
        (object search, object result) searchResultPair8,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,
                searchResultPair6,
                searchResultPair7,
                searchResultPair8,}),
            Resolve(@default));

    /// <inheritdoc cref="Decode(object, System.ValueTuple{object, object}[], object)"/>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        (object search, object result) searchResultPair6,
        (object search, object result) searchResultPair7,
        (object search, object result) searchResultPair8,
        (object search, object result) searchResultPair9,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,
                searchResultPair6,
                searchResultPair7,
                searchResultPair8,
                searchResultPair9,}),
            Resolve(@default));

    /// <inheritdoc cref="Decode(object, System.ValueTuple{object, object}[], object)"/>
    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair1,
        (object search, object result) searchResultPair2,
        (object search, object result) searchResultPair3,
        (object search, object result) searchResultPair4,
        (object search, object result) searchResultPair5,
        (object search, object result) searchResultPair6,
        (object search, object result) searchResultPair7,
        (object search, object result) searchResultPair8,
        (object search, object result) searchResultPair9,
        (object search, object result) searchResultPair10,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] {
                searchResultPair1,
                searchResultPair2,
                searchResultPair3,
                searchResultPair4,
                searchResultPair5,
                searchResultPair6,
                searchResultPair7,
                searchResultPair8,
                searchResultPair9,
                searchResultPair10,}),
            Resolve(@default));

    /// <summary>
    /// Begins a <c>DELETE FROM <paramref name="table"/></c> statement; continue
    /// with <c>.Where(...)</c> to restrict the rows removed.
    /// </summary>
    /// <param name="table">The table to delete rows from.</param>
    /// <returns>A delete builder positioned to accept a <c>WHERE</c> clause.</returns>
    public static IDeleteBuilderDelete DeleteFrom(DbTableBase table)
        => new DeleteBuilder(new DeleteClause(table));

    /// <summary>
    /// The <c>DENSE_RANK()</c> analytic function (rank within the window with no
    /// gaps after ties). Complete the call with <c>.Over(...)</c>.
    /// </summary>
    /// <returns>The <c>DENSE_RANK()</c> analytic function expression.</returns>
    public static AnalyticDenseRankFunction DenseRank() => new();

    /// <summary>
    /// The <c>DISTINCT</c> keyword passed to an aggregate (<see cref="Count(object)"/>,
    /// <see cref="Sum(object)"/>, <see cref="Avg(object)"/>,
    /// <see cref="GroupConcat(DistinctKeyword, object)"/>) or to <c>Select(...)</c>
    /// to deduplicate.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DistinctKeyword Distinct => new();

    /// <summary>
    /// PostgreSQL's <c>DISTINCT ON (<paramref name="expressions"/>)</c> select
    /// prefix: keeps one row per distinct combination of
    /// <paramref name="expressions"/> (the first per the query's <c>ORDER BY</c>).
    /// Pass to <c>Select(DistinctOn(...), ...)</c>.
    /// </summary>
    /// <param name="expressions">The expressions to deduplicate on; at least one.</param>
    /// <returns>A <c>DISTINCT ON (...)</c> select prefix.</returns>
    /// <remarks>PostgreSQL syntax; emitted faithfully on every dialect.</remarks>
    public static DistinctOnKeyword DistinctOn(params object[] expressions) =>
        new(Resolve(expressions));

    /// <summary>
    /// The <c>DUAL</c> dummy table (MySQL and Oracle), for selecting expressions
    /// without a real table (<c>SELECT ... FROM DUAL</c>).
    /// </summary>
    /// <remarks>On other dialects use a <c>FROM</c>-less <c>Select(...)</c> instead.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DualTable Dual => new();
}
