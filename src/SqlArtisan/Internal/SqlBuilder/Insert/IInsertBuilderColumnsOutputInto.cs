namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT INTO table (col, ...) OUTPUT ...</c> (SQL Server):
/// optionally redirect the output <c>INTO</c> a table, then supply the rows.
/// </summary>
public interface IInsertBuilderColumnsOutputInto : IInsertBuilderColumns
{
    /// <summary>
    /// Appends <c>INTO table (col, ...)</c> (SQL Server), redirecting the
    /// <c>OUTPUT</c> rows into <paramref name="table"/> instead of returning them
    /// to the caller.
    /// </summary>
    /// <param name="table">The table the output rows are inserted into.</param>
    /// <param name="columns">The columns to populate, in output order; omit to target the table's columns positionally.</param>
    /// <returns>The builder positioned to supply the rows with <c>Values(...)</c> / a <c>SELECT</c>.</returns>
    IInsertBuilderColumns Into(DbTableBase table, params DbColumn[] columns);
}
