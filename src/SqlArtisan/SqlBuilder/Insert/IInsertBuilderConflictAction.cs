namespace SqlArtisan;

/// <summary>
/// The state after an <c>ON CONFLICT</c> action (PostgreSQL/SQLite): add <c>RETURNING</c> or
/// build. Neither engine takes <c>RETURNING ... INTO</c>, so no <c>Into(...)</c> follows.
/// </summary>
public interface IInsertBuilderConflictAction : ISqlBuilder
{
    /// <summary>
    /// Appends <c>RETURNING expr, ...</c> (PostgreSQL, SQLite) to read values from the inserted
    /// or updated row.
    /// </summary>
    /// <param name="expressions">The columns or expressions to return; an alias renders as written.</param>
    /// <returns>The builder, ready to build.</returns>
    /// <exception cref="ArgumentException">No expressions were supplied.</exception>
    ISqlBuilder Returning(params object[] expressions);
}
