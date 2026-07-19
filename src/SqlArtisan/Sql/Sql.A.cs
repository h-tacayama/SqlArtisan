using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

/// <summary>
/// The entry point for building type-safe SQL. Its static factory methods produce
/// the SQL functions, expressions, predicates, clauses, and statement builders
/// (<c>Select</c>, <c>InsertInto</c>, <c>Update</c>, <c>DeleteFrom</c>,
/// <c>MergeInto</c>, <c>With</c>) that compose into a query. Import with
/// <c>using static SqlArtisan.Sql;</c> to write the factories unqualified.
/// </summary>
public static partial class Sql
{
    /// <summary>
    /// The <c>ABS(<paramref name="expr"/>)</c> function (absolute value of the argument).
    /// </summary>
    /// <param name="expr">The numeric expression to take the absolute value of.</param>
    /// <returns>An <see cref="AbsFunction"/> emitting <c>ABS(expr)</c>.</returns>
    public static AbsFunction Abs(object expr) => new(Resolve(expr));

    /// <summary>
    /// The <c>ADD_MONTHS(<paramref name="dateTime"/>, <paramref name="months"/>)</c>
    /// function (the date/time shifted forward by the given number of months).
    /// </summary>
    /// <remarks>
    /// Dialect-specific (Oracle). On SQL Server use
    /// <see cref="Dateadd(DateTimePart, object, object)"/> with the month part instead.
    /// </remarks>
    /// <param name="dateTime">The date/time expression to shift.</param>
    /// <param name="months">The number of months to add.</param>
    /// <returns>An <see cref="AddMonthsFunction"/> emitting
    /// <c>ADD_MONTHS(dateTime, months)</c>.</returns>
    public static AddMonthsFunction AddMonths(object dateTime, object months) =>
        new(Resolve(dateTime), Resolve(months));

    /// <summary>
    /// The <c>ALL (subquery)</c> quantified comparison operator: the comparison must
    /// hold for every row returned by the subquery.
    /// Use with a comparison operator — e.g. <c>col &gt; All(subquery)</c>.
    /// </summary>
    /// <param name="subquery">A <c>SELECT</c> builder returning a single column.</param>
    /// <returns>A quantified-subquery expression emitting <c>ALL (SELECT ...)</c>.</returns>
    /// <remarks>SQLite's grammar has no quantified comparisons; the other dialects accept it.</remarks>
    public static QuantifiedSubquery All(ISubquery subquery) =>
        new(Keywords.All, subquery);

    /// <summary>
    /// The <c>ALL (array)</c> quantified comparison operator: the comparison must
    /// hold for every element of the array expression (PostgreSQL).
    /// Use with a comparison operator — e.g. <c>col &lt; All(BindArray(values))</c>.
    /// </summary>
    /// <param name="array">The array expression — an <see cref="BindArrayValue"/>,
    /// an <c>ARRAY[...]</c> constructor, or an array-typed column.</param>
    /// <returns>A quantified expression emitting <c>ALL (array)</c>.</returns>
    public static QuantifiedExpression All(SqlExpression array) =>
        new(Keywords.All, array);

    /// <summary>
    /// The <c>ANY (subquery)</c> quantified comparison operator: the comparison must
    /// hold for at least one row returned by the subquery.
    /// Use with a comparison operator — e.g. <c>col &gt; Any(subquery)</c>.
    /// </summary>
    /// <param name="subquery">A <c>SELECT</c> builder returning a single column.</param>
    /// <returns>A quantified-subquery expression emitting <c>ANY (SELECT ...)</c>.</returns>
    /// <remarks>SQLite's grammar has no quantified comparisons; the other dialects accept it.</remarks>
    public static QuantifiedSubquery Any(ISubquery subquery) =>
        new(Keywords.Any, subquery);

    /// <summary>
    /// The <c>ANY (array)</c> quantified comparison operator: the comparison must
    /// hold for at least one element of the array expression (PostgreSQL).
    /// Use with a comparison operator — e.g. <c>col == Any(BindArray(values))</c>.
    /// </summary>
    /// <param name="array">The array expression — an <see cref="BindArrayValue"/>,
    /// an <c>ARRAY[...]</c> constructor, or an array-typed column.</param>
    /// <returns>A quantified expression emitting <c>ANY (array)</c>.</returns>
    public static QuantifiedExpression Any(SqlExpression array) =>
        new(Keywords.Any, array);

    /// <summary>
    /// The <c>ARRAY[elements]</c> array constructor: an array value from the
    /// listed elements (PostgreSQL).
    /// </summary>
    /// <param name="elements">The array elements; at least one.</param>
    /// <returns>An <see cref="ArrayConstructorExpression"/> emitting <c>ARRAY[elements]</c>.</returns>
    public static ArrayConstructorExpression Array(params object[] elements)
    {
        CollectionGuard.ThrowIfEmpty(elements, "ARRAY[...] requires at least one element.");
        return new(Resolve(elements));
    }

    /// <summary>
    /// The array containment predicate <c>leftArray &lt;@ rightArray</c>: whether
    /// every element of <paramref name="leftArray"/> is also an element of
    /// <paramref name="rightArray"/> (PostgreSQL).
    /// </summary>
    /// <param name="leftArray">The array that must be contained.</param>
    /// <param name="rightArray">The array to search.</param>
    /// <returns>An <see cref="ArrayContainedByCondition"/> emitting <c>leftArray &lt;@ rightArray</c>.</returns>
    public static ArrayContainedByCondition ArrayContainedBy(object leftArray, object rightArray) =>
        new(Resolve(leftArray), Resolve(rightArray));

    /// <summary>
    /// The array containment predicate <c>leftArray @&gt; rightArray</c>: whether
    /// every element of <paramref name="rightArray"/> is also an element of
    /// <paramref name="leftArray"/> (PostgreSQL).
    /// </summary>
    /// <param name="leftArray">The array to search.</param>
    /// <param name="rightArray">The array that must be contained.</param>
    /// <returns>An <see cref="ArrayContainsCondition"/> emitting <c>leftArray @&gt; rightArray</c>.</returns>
    public static ArrayContainsCondition ArrayContains(object leftArray, object rightArray) =>
        new(Resolve(leftArray), Resolve(rightArray));

    /// <summary>
    /// The array overlap predicate <c>leftArray &amp;&amp; rightArray</c>: whether
    /// the arrays have at least one element in common (PostgreSQL).
    /// </summary>
    /// <param name="leftArray">The first array.</param>
    /// <param name="rightArray">The second array.</param>
    /// <returns>An <see cref="ArrayOverlapsCondition"/> emitting <c>leftArray &amp;&amp; rightArray</c>.</returns>
    public static ArrayOverlapsCondition ArrayOverlaps(object leftArray, object rightArray) =>
        new(Resolve(leftArray), Resolve(rightArray));

    /// <summary>
    /// The bare <c>*</c> select item (<c>SELECT *</c>: every column of every table
    /// in <c>FROM</c>). Valid only in a <c>SELECT</c> or <c>RETURNING</c> list;
    /// for one table's columns use the table's <see cref="TableReference.Asterisk"/>.
    /// </summary>
    /// <remarks>Do not write <c>Select("*")</c> — a string is always a bind value,
    /// so it emits <c>SELECT :0</c> returning the literal <c>'*'</c>.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static AsteriskMarker Asterisk => new();

    /// <summary>
    /// The <c>AVG(<paramref name="expr"/>)</c> aggregate function (mean of the argument
    /// across the group).
    /// </summary>
    /// <param name="expr">The numeric expression to average.</param>
    /// <returns>An <see cref="AvgFunction"/> emitting <c>AVG(expr)</c>.</returns>
    public static AvgFunction Avg(object expr) =>
        new(Resolve(expr));

    /// <inheritdoc cref="Avg(object)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// restricting the average to distinct values.</param>
    /// <param name="expr">The numeric expression to average.</param>
    /// <returns>An <see cref="AvgFunction"/> emitting <c>AVG(DISTINCT expr)</c>.</returns>
    public static AvgFunction Avg(DistinctKeyword distinct, object expr) =>
        new(distinct, Resolve(expr));
}
