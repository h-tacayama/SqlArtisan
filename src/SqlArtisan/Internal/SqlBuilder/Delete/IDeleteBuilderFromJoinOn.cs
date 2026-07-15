namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>JOIN</c> inside a <c>DELETE alias FROM ...</c>: supply
/// the join predicate with <c>On(...)</c> or <c>Using(...)</c>.
/// </summary>
public interface IDeleteBuilderFromJoinOn
{
    /// <summary>
    /// Appends <c>ON condition</c> as the join predicate.
    /// </summary>
    /// <param name="condition">The join condition; literals it contains are auto-parameterized.</param>
    /// <returns>The builder back in the <c>FROM</c> state, ready for further joins, <c>WHERE</c>, <c>RETURNING</c>, or build.</returns>
    IDeleteBuilderFrom On(SqlCondition condition);

    /// <summary>
    /// Appends <c>USING (column, ...)</c> as the join predicate, matching rows where every listed column is equal.
    /// </summary>
    /// <param name="column">The first shared column to match on.</param>
    /// <param name="additionalColumns">Further shared columns, all matched with equality.</param>
    /// <returns>The builder back in the <c>FROM</c> state, ready for further joins, <c>WHERE</c>, <c>RETURNING</c>, or build.</returns>
    IDeleteBuilderFrom Using(DbColumn column, params DbColumn[] additionalColumns);
}
