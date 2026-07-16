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
    /// arithmetic native to those dialects.
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
    /// Server form.
    /// </remarks>
    public static DatediffFunction Datediff(
        DateTimePart datepart,
        object startDate,
        object endDate) => new(
            datepart,
            Resolve(startDate),
            Resolve(endDate));

    /// <summary>
    /// The <c>DATE_FORMAT(<paramref name="date"/>, <paramref name="format"/>)</c>
    /// function: <paramref name="date"/> rendered as a string per
    /// <paramref name="format"/>'s strftime-style specifiers (<c>%Y</c>,
    /// <c>%m</c>, <c>%d</c>, ...).
    /// </summary>
    /// <param name="date">The date/time value to format.</param>
    /// <param name="format">The strftime-style format string.</param>
    /// <returns>The <c>DATE_FORMAT</c> function expression.</returns>
    /// <remarks>MySQL syntax.</remarks>
    public static DateFormatFunction DateFormat(object date, object format) =>
        new(Resolve(date), Resolve(format));

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
    /// </remarks>
    public static DateTruncFunction DateTrunc(DateTimePart datepart, object source) =>
        new(datepart, Resolve(source));

    /// <summary>
    /// The <c>DATETRUNC(<paramref name="datepart"/>, <paramref name="date"/>)</c>
    /// function: <paramref name="date"/> truncated down to
    /// <paramref name="datepart"/> precision.
    /// </summary>
    /// <param name="datepart">The precision to truncate to.</param>
    /// <param name="date">The date/time value to truncate.</param>
    /// <returns>The <c>DATETRUNC</c> function expression.</returns>
    /// <remarks>
    /// SQL Server syntax (SQL Server 2022+; use <see cref="Format(object, object)"/>
    /// on earlier versions).
    /// </remarks>
    public static DatetruncFunction Datetrunc(DateTimePart datepart, object date) =>
        new(datepart, Resolve(date));

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
    public static IDeleteBuilderDeleteOutput DeleteFrom(DbTableBase table)
    {
        DmlJoinState state = new();
        DeleteClause deleteClause = new(table, state);
        return new DeleteBuilder(table, state, deleteClause);
    }

    /// <summary>
    /// References <paramref name="column"/> of the <c>DELETED</c> pseudo-table in
    /// a SQL Server <c>OUTPUT</c> clause — the row's pre-image before a
    /// <c>DELETE</c> or <c>UPDATE</c>. Renders as <c>DELETED.col</c>.
    /// </summary>
    /// <param name="column">The target-table column whose deleted value to read.</param>
    /// <returns>A <c>DELETED.col</c> reference.</returns>
    /// <remarks>SQL Server syntax, valid only inside <c>Output(...)</c>.</remarks>
    public static DeletedColumn Deleted(DbColumn column) => new(column);

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
    /// The <c>||</c> concatenation operator: <c>(<paramref name="primary"/> || <paramref name="secondary"/> || ...)</c>,
    /// chaining any number of arguments without nesting.
    /// </summary>
    /// <param name="primary">The first string expression.</param>
    /// <param name="secondary">The second string expression.</param>
    /// <param name="others">Any further string expressions, chained in order.</param>
    /// <returns>A <see cref="DoublePipeOperator"/> emitting <c>(a || b || ...)</c>.</returns>
    /// <remarks>
    /// Oracle, PostgreSQL, and SQLite (every version) syntax. On MySQL, <c>||</c> is
    /// <em>logical OR</em> under the default <c>sql_mode</c> — valid SQL with silently
    /// different semantics — so use <see cref="Concat(object, object)"/> /
    /// <see cref="Concat(object, object, object, object[])"/> there instead. SQL Server
    /// rejects <c>||</c> entirely; its concatenation operator is <c>+</c>, already
    /// emitted by the existing <c>+</c> operator on <see cref="SqlExpression"/>.
    /// </remarks>
    public static DoublePipeOperator DoublePipe(object primary, object secondary, params object[] others) =>
        new(Resolve(primary), Resolve(secondary), Resolve(others));

    /// <summary>
    /// The <c>DUAL</c> dummy table (MySQL and Oracle), for selecting expressions
    /// without a real table (<c>SELECT ... FROM DUAL</c>).
    /// </summary>
    /// <remarks>On other dialects use a <c>FROM</c>-less <c>Select(...)</c> instead.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static DualTable Dual => new();
}
