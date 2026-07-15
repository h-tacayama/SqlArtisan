namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>DELETE alias FROM target ...</c> (the SQL Server / MySQL
/// form, where the target is re-listed in <c>FROM</c>): join further tables,
/// filter with <c>WHERE</c>, add <c>RETURNING</c>, or build.
/// </summary>
public interface IDeleteBuilderFrom : ISqlBuilder, IReturning
{
    /// <summary>
    /// Appends <c>FULL JOIN table</c>; supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to full-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IDeleteBuilderFromJoinOn FullJoin(TableReference table);

    /// <summary>
    /// Appends <c>INNER JOIN table</c>; supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to inner-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IDeleteBuilderFromJoinOn InnerJoin(TableReference table);

    /// <summary>
    /// Appends <c>LEFT JOIN table</c>; supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to left-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IDeleteBuilderFromJoinOn LeftJoin(TableReference table);

    /// <summary>
    /// Appends <c>RIGHT JOIN table</c>; supply its predicate with the following <c>On(...)</c>.
    /// </summary>
    /// <param name="table">The table reference to right-join.</param>
    /// <returns>The builder positioned to supply the join predicate.</returns>
    IDeleteBuilderFromJoinOn RightJoin(TableReference table);

    /// <summary>
    /// Appends <c>WHERE condition</c> to restrict which rows are deleted.
    /// </summary>
    /// <param name="condition">The row filter; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned for <c>RETURNING</c> or build.</returns>
    IDeleteBuilderWhere Where(SqlCondition condition);
}
