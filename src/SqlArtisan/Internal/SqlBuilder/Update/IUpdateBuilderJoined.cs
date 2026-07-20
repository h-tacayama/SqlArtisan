namespace SqlArtisan.Internal;

/// <summary>
/// The state after a joined <c>UPDATE target JOIN aux ON ...</c> (the MySQL
/// multi-table form): add another join, or supply the <c>SET</c> assignments.
/// </summary>
public interface IUpdateBuilderJoined
{
    /// <summary>
    /// Appends <c>INNER JOIN table</c>; supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to inner-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IUpdateBuilderJoinOn InnerJoin(TableReference table);

    /// <summary>
    /// Appends <c>LEFT JOIN table</c>; supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to left-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IUpdateBuilderJoinOn LeftJoin(TableReference table);

    /// <summary>
    /// Appends <c>RIGHT JOIN table</c>; supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to right-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IUpdateBuilderJoinOn RightJoin(TableReference table);

    /// <summary>
    /// Appends <c>SET col = value, ...</c>; the target columns are alias-qualified for the joined form.
    /// </summary>
    /// <param name="assignments">The per-column updates; each left side names a target column and each right side its new value (literals are auto-parameterized).</param>
    /// <returns>The builder positioned for <c>WHERE</c>, <c>RETURNING</c>, or build.</returns>
    IUpdateBuilderJoinedSet Set(params EqualityBasedCondition[] assignments);
}
