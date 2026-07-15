namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>DELETE FROM table</c>: join other tables with
/// <c>USING</c> (PostgreSQL) or <c>FROM</c> (SQL Server / MySQL, re-listing the
/// target), narrow with <c>WHERE</c>, add <c>RETURNING</c>, or build.
/// </summary>
public interface IDeleteBuilderDelete : ISqlBuilder, IReturning
{
    /// <summary>
    /// Appends <c>FROM target, ...</c> for the SQL Server / MySQL joined delete;
    /// re-list the target table here (its alias then leads the statement) and
    /// join the other tables.
    /// </summary>
    /// <param name="tables">The tables the delete joins, including the re-listed target.</param>
    /// <returns>The builder positioned to join further tables, filter with <c>WHERE</c>, or build.</returns>
    IDeleteBuilderFrom From(params TableReference[] tables);

    /// <summary>
    /// Appends <c>USING table, ...</c> for PostgreSQL's <c>DELETE ... USING</c>;
    /// relate the target to these tables in the <c>WHERE</c> predicate.
    /// </summary>
    /// <param name="tables">The tables the delete draws from.</param>
    /// <returns>The builder positioned for <c>WHERE</c>, <c>RETURNING</c>, or build.</returns>
    IDeleteBuilderUsing Using(params TableReference[] tables);

    /// <summary>
    /// Appends <c>WHERE condition</c> to restrict which rows are deleted.
    /// </summary>
    /// <param name="condition">The row filter; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned for <c>RETURNING</c> or build.</returns>
    IDeleteBuilderWhere Where(SqlCondition condition);
}
