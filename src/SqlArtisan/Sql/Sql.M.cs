using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// Starts a <c>MERGE INTO target</c> statement (Oracle / SQL Server, and
    /// PostgreSQL 15+). Continue with <c>Using(...).On(...)</c> and one or more
    /// <c>WhenMatched</c> / <c>WhenNotMatched</c> branches.
    /// </summary>
    /// <param name="target">The table to merge rows into.</param>
    /// <returns>A merge builder positioned to accept <c>Using(...).On(...)</c>.</returns>
    /// <remarks>The emitted SQL is per-dialect: SQL Server appends the required
    /// terminating semicolon and supports <c>WHEN NOT MATCHED BY SOURCE</c>.</remarks>
    public static IMergeBuilderTarget MergeInto(DbTableBase target) =>
        new MergeBuilder(new MergeIntoClause(target));

    /// <summary>
    /// The <c>MAX(<paramref name="expr"/>)</c> aggregate: the largest value of
    /// <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The value to aggregate.</param>
    /// <returns>The MAX construct.</returns>
    public static MaxFunction Max(object expr) => new(Resolve(expr));

    /// <summary>
    /// The <c>MIN(<paramref name="expr"/>)</c> aggregate: the smallest value of
    /// <paramref name="expr"/> across the group.
    /// </summary>
    /// <param name="expr">The value to aggregate.</param>
    /// <returns>The MIN construct.</returns>
    public static MinFunction Min(object expr) => new(Resolve(expr));

    /// <summary>
    /// The <c>MOD(<paramref name="dividend"/>, <paramref name="divisor"/>)</c>
    /// function: the remainder of <paramref name="dividend"/> divided by
    /// <paramref name="divisor"/>.
    /// </summary>
    /// <param name="dividend">The number being divided.</param>
    /// <param name="divisor">The number to divide by.</param>
    /// <returns>The MOD construct.</returns>
    public static ModFunction Mod(
        object dividend,
        object divisor) => new(
            Resolve(dividend),
            Resolve(divisor));

    /// <summary>
    /// The <c>MONTHS_BETWEEN(<paramref name="date1"/>, <paramref name="date2"/>)</c>
    /// function: the number of months between <paramref name="date1"/>
    /// and <paramref name="date2"/>.
    /// </summary>
    /// <param name="date1">The later date.</param>
    /// <param name="date2">The earlier date.</param>
    /// <returns>The MONTHS_BETWEEN construct.</returns>
    /// <remarks>Oracle syntax.</remarks>
    public static MonthsBetweenFunction MonthsBetween(
        object date1,
        object date2) => new(
            Resolve(date1),
            Resolve(date2));
}
