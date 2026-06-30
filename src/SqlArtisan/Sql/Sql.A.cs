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
    public static AddMonthsFunction AddMonths(
        object dateTime,
        object months) => new(
            Resolve(dateTime),
            Resolve(months));

    /// <summary>
    /// The <c>ALL (subquery)</c> quantified comparison operator: the comparison must
    /// hold for every row returned by the subquery.
    /// Use with a comparison operator — e.g. <c>col &gt; All(subquery)</c>.
    /// </summary>
    /// <param name="subquery">A <c>SELECT</c> builder returning a single column.</param>
    /// <returns>A quantified-subquery expression emitting <c>ALL (SELECT ...)</c>.</returns>
    public static QuantifiedSubquery All(ISubquery subquery) =>
        new(Keywords.All, subquery);

    /// <summary>
    /// The <c>ANY (subquery)</c> quantified comparison operator: the comparison must
    /// hold for at least one row returned by the subquery.
    /// Use with a comparison operator — e.g. <c>col &gt; Any(subquery)</c>.
    /// </summary>
    /// <param name="subquery">A <c>SELECT</c> builder returning a single column.</param>
    /// <returns>A quantified-subquery expression emitting <c>ANY (SELECT ...)</c>.</returns>
    public static QuantifiedSubquery Any(ISubquery subquery) =>
        new(Keywords.Any, subquery);

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
