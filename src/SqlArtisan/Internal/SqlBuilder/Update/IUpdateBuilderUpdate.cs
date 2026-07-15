namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>UPDATE table</c>: supply the column assignments with
/// <c>SET</c>, or begin MySQL's multi-table form with a <c>JOIN</c>.
/// </summary>
public interface IUpdateBuilderUpdate : ISqlBuilder
{
    /// <summary>
    /// Appends <c>FULL JOIN table</c> (MySQL multi-table <c>UPDATE</c>); supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to full-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IUpdateBuilderJoinOn FullJoin(TableReference table);

    /// <summary>
    /// Appends <c>INNER JOIN table</c> (MySQL multi-table <c>UPDATE</c>); supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to inner-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IUpdateBuilderJoinOn InnerJoin(TableReference table);

    /// <summary>
    /// Appends <c>LEFT JOIN table</c> (MySQL multi-table <c>UPDATE</c>); supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to left-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IUpdateBuilderJoinOn LeftJoin(TableReference table);

    /// <summary>
    /// Appends <c>RIGHT JOIN table</c> (MySQL multi-table <c>UPDATE</c>); supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to right-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IUpdateBuilderJoinOn RightJoin(TableReference table);

    /// <summary>
    /// Appends <c>SET col = value, ...</c> from <c>column == value</c> assignments.
    /// </summary>
    /// <param name="assignments">The per-column updates; each left side names a column and each right side its new value (literals are auto-parameterized).</param>
    /// <returns>The builder positioned for <c>OUTPUT</c>, <c>WHERE</c>, <c>RETURNING</c>, or build.</returns>
    IUpdateBuilderSetOutput Set(params EqualityBasedCondition[] assignments);
}
