namespace SqlArtisan.Internal;

/// <summary>
/// The state after an <c>INNER</c>/<c>LEFT</c>/<c>RIGHT</c>/<c>FULL JOIN</c>: supply its <c>ON</c> predicate.
/// </summary>
public interface ISelectBuilderJoin : ISqlBuilder, IForUpdate
{
    /// <summary>
    /// Appends <c>ON condition</c> as the join predicate.
    /// </summary>
    /// <param name="condition">The join condition; literals it contains are auto-parameterized.</param>
    /// <returns>The builder back in the <c>FROM</c> state, ready for further joins, <c>WHERE</c>, grouping, ordering, pagination, or build.</returns>
    ISelectBuilderFrom On(SqlCondition condition);

    /// <summary>
    /// Appends <c>USING (column, ...)</c> as the join predicate, matching rows where every listed column
    /// is equal (and shared, unqualified, in the result) instead of an explicit <c>ON</c> comparison.
    /// </summary>
    /// <param name="column">The first (and possibly only) shared column to match on.</param>
    /// <param name="additionalColumns">Further shared columns, all matched with equality.</param>
    /// <returns>The builder back in the <c>FROM</c> state, ready for further joins, <c>WHERE</c>, grouping, ordering, pagination, or build.</returns>
    /// <remarks>SQL Server has no <c>JOIN ... USING</c> — spell the equivalent with <c>On(...)</c> there.</remarks>
    ISelectBuilderFrom Using(DbColumn column, params DbColumn[] additionalColumns);
}
