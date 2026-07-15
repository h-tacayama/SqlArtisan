namespace SqlArtisan.Internal;

/// <summary>
/// The state after a joined <c>UPDATE target JOIN aux</c> (the MySQL multi-table
/// form): supply the join predicate with <c>On(...)</c> or <c>Using(...)</c>.
/// </summary>
public interface IUpdateBuilderJoinOn
{
    /// <summary>
    /// Appends <c>ON condition</c> as the join predicate.
    /// </summary>
    /// <param name="condition">The join condition; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned to add another join or the <c>SET</c> assignments.</returns>
    IUpdateBuilderJoined On(SqlCondition condition);

    /// <summary>
    /// Appends <c>USING (column, ...)</c> as the join predicate, matching rows where every listed column is equal.
    /// </summary>
    /// <param name="column">The first shared column to match on.</param>
    /// <param name="additionalColumns">Further shared columns, all matched with equality.</param>
    /// <returns>The builder positioned to add another join or the <c>SET</c> assignments.</returns>
    IUpdateBuilderJoined Using(DbColumn column, params DbColumn[] additionalColumns);
}
