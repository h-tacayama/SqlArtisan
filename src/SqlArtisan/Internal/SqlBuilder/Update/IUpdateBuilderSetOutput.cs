namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>UPDATE ... SET</c>: as <see cref="IUpdateBuilderSet"/>, and
/// additionally return affected-row data with SQL Server's <c>OUTPUT</c>.
/// </summary>
public interface IUpdateBuilderSetOutput : IUpdateBuilderSet
{
    /// <summary>
    /// Appends <c>OUTPUT expr, ...</c> (SQL Server) to read values from the updated
    /// rows — reference the <c>INSERTED</c> / <c>DELETED</c> pseudo-tables via
    /// <see cref="Sql.Inserted(DbColumn)"/> / <see cref="Sql.Deleted(DbColumn)"/>.
    /// </summary>
    /// <param name="items">The columns or expressions to output; literals are auto-parameterized and <c>.As(...)</c> aliases are allowed.</param>
    /// <returns>The builder positioned to redirect the output <c>Into(...)</c> an archive table, or continue with <c>WHERE</c> / <c>FROM</c> / build.</returns>
    IUpdateBuilderOutputInto Output(params object[] items);
}
