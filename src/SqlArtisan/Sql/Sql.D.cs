using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// Adds <paramref name="number"/> units of <paramref name="datepart"/> to
    /// <paramref name="dateTime"/>, emitting SQL Server's
    /// <c>DATEADD(datepart, number, date)</c>. Pass a negative
    /// <paramref name="number"/> to subtract. For Oracle use
    /// <see cref="AddMonths(object, object)"/>; PostgreSQL/MySQL use interval
    /// arithmetic native to those dialects.
    /// <para><see cref="DateTimePart"/> is a superset shared with EXTRACT/DATEPART;
    /// only the dateparts SQL Server's <c>DATEADD</c> accepts are valid here.</para>
    /// </summary>
    public static DateaddFunction Dateadd(
        DateTimePart datepart,
        object number,
        object dateTime) => new(
            datepart,
            Resolve(number),
            Resolve(dateTime));

    /// <summary>
    /// Returns the number of <paramref name="datepart"/> boundaries crossed
    /// between <paramref name="startDate"/> and <paramref name="endDate"/>,
    /// emitting SQL Server's <c>DATEDIFF(datepart, startdate, enddate)</c>.
    /// Argument order and supported units are vendor-specific; this is the SQL
    /// Server form.
    /// <para><see cref="DateTimePart"/> is a superset shared with EXTRACT/DATEPART;
    /// only the dateparts SQL Server's <c>DATEDIFF</c> accepts are valid here.</para>
    /// </summary>
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
    /// Truncates <paramref name="source"/> down to <paramref name="datepart"/>
    /// precision, emitting PostgreSQL's <c>DATE_TRUNC('datepart', source)</c>.
    /// For Oracle use the date/time overload of <see cref="Trunc(object, object)"/>
    /// (<c>TRUNC(date, fmt)</c>).
    /// <para><see cref="DateTimePart"/> is a superset shared with EXTRACT/DATEPART;
    /// only the fields PostgreSQL's <c>date_trunc</c> accepts are valid here.</para>
    /// </summary>
    public static DateTruncFunction DateTrunc(
        DateTimePart datepart,
        object source) => new(datepart, Resolve(source));

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
    /// Oracle's <c>DUAL</c> dummy table, for selecting expressions without a real
    /// table (<c>SELECT ... FROM DUAL</c>).
    /// </summary>
    /// <remarks>This is Oracle-specific.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DualTable Dual => new();
}
