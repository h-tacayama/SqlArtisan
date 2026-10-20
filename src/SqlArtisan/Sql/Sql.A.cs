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
    /// <returns>An expression emitting <c>ABS(expr)</c>.</returns>
    public static SqlExpression Abs(object expr) => new AbsFunction(Resolve(expr));

    /// <summary>
    /// The <c>ADD_MONTHS(<paramref name="dateTime"/>, <paramref name="months"/>)</c>
    /// function (the date/time shifted forward by the given number of months).
    /// </summary>
    /// <remarks>Oracle syntax. SQL Server: <see cref="Dateadd(DateTimePart, object, object)"/>
    /// with the month part; MySQL/PostgreSQL: <see cref="Interval(object, DateTimePart)"/> or
    /// <see cref="IntervalLiteral(string, IntervalField)"/> with <c>+</c>.</remarks>
    /// <param name="dateTime">The date/time expression to shift.</param>
    /// <param name="months">The number of months to add.</param>
    /// <returns>An expression emitting
    /// <c>ADD_MONTHS(dateTime, months)</c>.</returns>
    public static SqlExpression AddMonths(object dateTime, object months) =>
        new AddMonthsFunction(Resolve(dateTime), Resolve(months));

    /// <summary>
    /// The <c>ALL (subquery)</c> quantified comparison operator: the comparison must hold for every
    /// row returned by the subquery. Valid only in the slots <see cref="QuantifiedExpression"/>
    /// lists, as in <c>col &gt; All(subquery)</c>.
    /// </summary>
    /// <param name="subquery">A <c>SELECT</c> builder returning a single column.</param>
    /// <returns>A quantified-subquery expression emitting <c>ALL (SELECT ...)</c>.</returns>
    /// <remarks>Not supported by SQLite — its grammar has no quantified comparisons.</remarks>
    public static QuantifiedSubquery All(ISubquery subquery) =>
        new(Keywords.All, subquery);

    /// <summary>
    /// The <c>ALL (array)</c> quantified comparison operator: the comparison must hold for every
    /// element of the array expression (PostgreSQL). Valid only in the slots
    /// <see cref="QuantifiedExpression"/> lists, as in <c>col &lt; All(BindArray(values))</c>.
    /// </summary>
    /// <param name="array">The array expression — a <see cref="BindArrayValue"/>,
    /// an <c>ARRAY[...]</c> constructor, or an array-typed column.</param>
    /// <returns>A quantified expression emitting <c>ALL (array)</c>.</returns>
    public static QuantifiedExpression All(SqlExpression array) =>
        new(Keywords.All, array);

    /// <summary>
    /// The <c>ANY (subquery)</c> quantified comparison operator: the comparison must hold for at
    /// least one row returned by the subquery. Valid only in the slots
    /// <see cref="QuantifiedExpression"/> lists, as in <c>col &gt; Any(subquery)</c>.
    /// </summary>
    /// <param name="subquery">A <c>SELECT</c> builder returning a single column.</param>
    /// <returns>A quantified-subquery expression emitting <c>ANY (SELECT ...)</c>.</returns>
    /// <remarks>Not supported by SQLite — its grammar has no quantified comparisons.</remarks>
    public static QuantifiedSubquery Any(ISubquery subquery) =>
        new(Keywords.Any, subquery);

    /// <summary>
    /// The <c>ANY (array)</c> quantified comparison operator: the comparison must hold for at least
    /// one element of the array expression (PostgreSQL). Valid only in the slots
    /// <see cref="QuantifiedExpression"/> lists, as in <c>col == Any(BindArray(values))</c>.
    /// </summary>
    /// <param name="array">The array expression — a <see cref="BindArrayValue"/>,
    /// an <c>ARRAY[...]</c> constructor, or an array-typed column.</param>
    /// <returns>A quantified expression emitting <c>ANY (array)</c>.</returns>
    public static QuantifiedExpression Any(SqlExpression array) =>
        new(Keywords.Any, array);

    /// <summary>
    /// The <c>ARRAY[elements]</c> array constructor: an array value from the
    /// listed elements (PostgreSQL).
    /// </summary>
    /// <param name="elements">The array elements; at least one.</param>
    /// <returns>An expression emitting <c>ARRAY[elements]</c>.</returns>
    public static SqlExpression Array(params object[] elements)
    {
        CollectionGuard.ThrowIfEmpty(
            elements, nameof(elements), "ARRAY[...] requires at least one element.");
        return new ArrayConstructorExpression(Resolve(elements));
    }

    /// <summary>
    /// The array containment predicate <c>leftArray &lt;@ rightArray</c>: whether
    /// every element of <paramref name="leftArray"/> is also an element of
    /// <paramref name="rightArray"/> (PostgreSQL).
    /// </summary>
    /// <param name="leftArray">The array that must be contained.</param>
    /// <param name="rightArray">The array to search.</param>
    /// <returns>A condition emitting <c>leftArray &lt;@ rightArray</c>.</returns>
    public static SqlCondition ArrayContainedBy(object leftArray, object rightArray) =>
        new ArrayContainedByCondition(
            ResolveCompared(leftArray, nameof(leftArray), Operators.ArrayContainedBy),
            ResolveQuantifiable(rightArray, nameof(rightArray), Operators.ArrayContainedBy));

    /// <summary>
    /// The array containment predicate <c>leftArray @&gt; rightArray</c>: whether
    /// every element of <paramref name="rightArray"/> is also an element of
    /// <paramref name="leftArray"/> (PostgreSQL).
    /// </summary>
    /// <param name="leftArray">The array to search.</param>
    /// <param name="rightArray">The array that must be contained.</param>
    /// <returns>A condition emitting <c>leftArray @&gt; rightArray</c>.</returns>
    public static SqlCondition ArrayContains(object leftArray, object rightArray) =>
        new ArrayContainsCondition(
            ResolveCompared(leftArray, nameof(leftArray), Operators.ArrayContains),
            ResolveQuantifiable(rightArray, nameof(rightArray), Operators.ArrayContains));

    /// <summary>
    /// The array overlap predicate <c>leftArray &amp;&amp; rightArray</c>: whether
    /// the arrays have at least one element in common (PostgreSQL).
    /// </summary>
    /// <param name="leftArray">The first array.</param>
    /// <param name="rightArray">The second array.</param>
    /// <returns>A condition emitting <c>leftArray &amp;&amp; rightArray</c>.</returns>
    public static SqlCondition ArrayOverlaps(object leftArray, object rightArray) =>
        new ArrayOverlapsCondition(
            ResolveCompared(leftArray, nameof(leftArray), Operators.ArrayOverlaps),
            ResolveQuantifiable(rightArray, nameof(rightArray), Operators.ArrayOverlaps));

    /// <summary>
    /// The bare <c>*</c> select item (every column of every table in <c>FROM</c>). Valid in
    /// a <c>SELECT</c>, <c>RETURNING</c>, or <c>OUTPUT</c> list and as <c>Count(Asterisk)</c>;
    /// for one table's columns use <see cref="TableReference.Asterisk"/>.
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
    /// <returns>An aggregate emitting <c>AVG(expr)</c>.</returns>
    public static UnfilteredAggregateFunction Avg(object expr) =>
        new AvgFunction(Resolve(expr));

    /// <inheritdoc cref="Avg(object)"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword (<see cref="Sql.Distinct"/>),
    /// restricting the average to distinct values.</param>
    /// <param name="expr">The numeric expression to average.</param>
    /// <returns>A <see cref="DistinctAggregateFunction"/> emitting <c>AVG(DISTINCT expr)</c>.</returns>
    public static DistinctAggregateFunction Avg(DistinctKeyword distinct, object expr) =>
        new(Keywords.Avg, distinct, Resolve(expr));
}
