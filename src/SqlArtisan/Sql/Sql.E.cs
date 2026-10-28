using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>ELSE <paramref name="thenExpr"/></c> arm of a CASE expression,
    /// passed to a <c>Sql.Case(...)</c> overload.
    /// </summary>
    /// <param name="thenExpr">The value returned when no <c>WHEN</c> matches.</param>
    /// <returns>The <c>ELSE</c> arm.</returns>
    public static CaseElseExpression Else(object thenExpr) =>
        new(Resolve(thenExpr, nameof(thenExpr)));

    /// <summary>
    /// References a column of the row proposed for insertion inside an UPSERT
    /// update clause (PostgreSQL/SQLite <c>EXCLUDED</c>, MySQL row alias).
    /// </summary>
    /// <param name="column">The conflicting-insert column to reference, a column of the
    /// <c>INSERT</c> target table.</param>
    /// <returns>An expression referencing the proposed row's
    /// value of <paramref name="column"/>.</returns>
    /// <remarks>MySQL (8.0.19+), PostgreSQL, and SQLite syntax — SqlArtisan
    /// always emits the row-alias form on MySQL. Oracle and SQL Server have no
    /// UPSERT excluded row — use <c>MERGE</c> there.</remarks>
    public static SqlExpression Excluded(DbColumn column) => new ExcludedColumn(column);

    /// <summary>
    /// The <c>EXISTS (<paramref name="subquery"/>)</c> predicate, true when
    /// <paramref name="subquery"/> returns at least one row.
    /// </summary>
    /// <param name="subquery">The subquery whose emptiness is tested.</param>
    /// <returns>The <c>EXISTS</c> condition.</returns>
    public static SqlCondition Exists(ISubquery subquery) => new ExistsCondition(subquery);

    /// <summary>
    /// The <c>EXP(<paramref name="expr"/>)</c> function: e raised to the power of
    /// <paramref name="expr"/>.
    /// </summary>
    /// <param name="expr">The exponent.</param>
    /// <returns>An <c>EXP</c> function expression.</returns>
    /// <remarks>Supported on every dialect (SQLite 3.35+).</remarks>
    public static SqlExpression Exp(object expr) =>
        new ExpFunction(Resolve(expr, nameof(expr)));

    /// <summary>
    /// The <c>EXTRACT(<paramref name="datepart"/> FROM <paramref name="source"/>)</c>
    /// function returning a single date/time field.
    /// </summary>
    /// <param name="datepart">The field of <paramref name="source"/> to return.</param>
    /// <param name="source">The date/time value to read the field from.</param>
    /// <returns>The <c>EXTRACT</c> function expression.</returns>
    /// <remarks>MySQL, Oracle, and PostgreSQL syntax; SQL Server's equivalent is
    /// <see cref="Datepart(DateTimePart, object)"/>.</remarks>
    public static SqlExpression Extract(DateTimePart datepart, object source) =>
        new ExtractFunction(datepart, Resolve(source, nameof(source)));
}
