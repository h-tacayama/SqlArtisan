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
    /// <para><see cref="Datepart"/> is a superset shared with EXTRACT/DATEPART;
    /// only the dateparts SQL Server's <c>DATEADD</c> accepts are valid here.</para>
    /// </summary>
    public static DateAddFunction DateAdd(
        Datepart datepart,
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
    /// <para><see cref="Datepart"/> is a superset shared with EXTRACT/DATEPART;
    /// only the dateparts SQL Server's <c>DATEDIFF</c> accepts are valid here.</para>
    /// </summary>
    public static DateDiffFunction DateDiff(
        Datepart datepart,
        object startDate,
        object endDate) => new(
            datepart,
            Resolve(startDate),
            Resolve(endDate));

    public static DatepartFunction Datepart(Datepart datepart, object source) =>
        new(datepart, Resolve(source));

    /// <summary>
    /// Truncates <paramref name="source"/> down to <paramref name="datepart"/>
    /// precision, emitting PostgreSQL's <c>DATE_TRUNC('datepart', source)</c>.
    /// For Oracle use the date/time overload of <see cref="Trunc(object, object)"/>
    /// (<c>TRUNC(date, fmt)</c>).
    /// <para><see cref="Datepart"/> is a superset shared with EXTRACT/DATEPART;
    /// only the fields PostgreSQL's <c>date_trunc</c> accepts are valid here.</para>
    /// </summary>
    public static DateTruncFunction DateTrunc(
        Datepart datepart,
        object source) => new(datepart, Resolve(source));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result)[] searchResultPairs,
        object @default) => new(
            Resolve(expr),
            Resolve(searchResultPairs),
            Resolve(@default));

    public static DecodeFunction Decode(
        object expr,
        (object search, object result) searchResultPair,
        object @default) => new(
            Resolve(expr),
            Resolve(new (object, object)[] { searchResultPair }),
            Resolve(@default));

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

    public static IDeleteBuilderDelete DeleteFrom(DbTableBase table)
        => new DeleteBuilder(new DeleteClause(table));

    public static AnalyticDenseRankFunction DenseRank() => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DistinctKeyword Distinct => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DualTable Dual => new();
}
