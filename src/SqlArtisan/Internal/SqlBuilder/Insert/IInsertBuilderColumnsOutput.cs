namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT INTO table (col, ...)</c>: as
/// <see cref="IInsertBuilderColumns"/>, and additionally return affected-row data
/// with SQL Server's <c>OUTPUT</c> (emitted before <c>VALUES</c>/<c>SELECT</c>).
/// </summary>
public interface IInsertBuilderColumnsOutput : IInsertBuilderColumns
{
    /// <summary>
    /// Appends <c>OUTPUT expr, ...</c> (SQL Server) to read values from the inserted
    /// rows — reference the <c>INSERTED</c> pseudo-table via
    /// <see cref="Sql.Inserted(DbColumn)"/>.
    /// </summary>
    /// <param name="items">The columns or expressions to output; literals are auto-parameterized and <c>.As(...)</c> aliases are allowed.</param>
    /// <returns>The builder positioned to redirect the output <c>Into(...)</c> an archive table, or supply the rows with <c>Values(...)</c> / a <c>SELECT</c>.</returns>
    IInsertBuilderColumnsOutputInto Output(params object[] items);
}
