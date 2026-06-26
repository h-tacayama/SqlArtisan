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
    /// Conditionally includes a <see cref="SqlCondition"/>: returns
    /// <paramref name="condition"/> when <paramref name="when"/> is
    /// <see langword="true"/>, otherwise an empty condition that emits nothing.
    /// </summary>
    /// <remarks>
    /// Use this to drop a predicate out of a <c>WHERE</c> clause based on a runtime
    /// flag without breaking the fluent chain.
    /// </remarks>
    /// <param name="when">When <see langword="true"/>, the condition is included; when
    /// <see langword="false"/>, it is omitted.</param>
    /// <param name="condition">The condition to include when <paramref name="when"/> is
    /// <see langword="true"/>.</param>
    /// <returns><paramref name="condition"/> when <paramref name="when"/> is
    /// <see langword="true"/>; otherwise an empty <see cref="SqlCondition"/> that emits
    /// nothing.</returns>
    public static SqlCondition ConditionIf(
        bool when,
        SqlCondition condition) =>
        when ? condition : new EmptyCondition();

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
