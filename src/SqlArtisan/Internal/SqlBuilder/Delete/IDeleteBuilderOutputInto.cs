namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>DELETE FROM table OUTPUT ...</c> (SQL Server): optionally
/// redirect the output <c>INTO</c> a table, or continue the statement.
/// </summary>
public interface IDeleteBuilderOutputInto : IDeleteBuilderDelete
{
    /// <summary>
    /// Appends <c>INTO table (col, ...)</c> (SQL Server), redirecting the
    /// <c>OUTPUT</c> rows into <paramref name="table"/> instead of returning them
    /// to the caller — the single-statement archive-then-delete form.
    /// </summary>
    /// <param name="table">The table the output rows are inserted into.</param>
    /// <param name="columns">The columns to populate, in output order; omit to target the table's columns positionally.</param>
    /// <returns>The builder positioned to continue with <c>WHERE</c> / <c>FROM</c> / <c>USING</c> / build.</returns>
    IDeleteBuilderDelete Into(DbTableBase table, params DbColumn[] columns);
}
