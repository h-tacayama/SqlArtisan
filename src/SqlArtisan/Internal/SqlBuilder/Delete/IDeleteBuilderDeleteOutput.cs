namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>DELETE FROM table</c>: as <see cref="IDeleteBuilderDelete"/>,
/// and additionally return affected-row data with SQL Server's <c>OUTPUT</c>.
/// </summary>
public interface IDeleteBuilderDeleteOutput : IDeleteBuilderDelete
{
    /// <summary>
    /// Appends <c>OUTPUT expr, ...</c> (SQL Server) to read values from the deleted
    /// rows — reference the <c>DELETED</c> pseudo-table via
    /// <see cref="Sql.Deleted(DbColumn)"/>.
    /// </summary>
    /// <param name="items">The columns or expressions to output; literals are auto-parameterized and <c>.As(...)</c> aliases are allowed.</param>
    /// <returns>The builder positioned to redirect the output <c>Into(...)</c> an archive table, or continue with <c>WHERE</c> / <c>FROM</c> / <c>USING</c> / build.</returns>
    IDeleteBuilderOutputInto Output(params object[] items);
}
