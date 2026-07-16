namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>UPDATE ... SET ... OUTPUT ...</c> (SQL Server): optionally
/// redirect the output <c>INTO</c> a table, or continue the statement.
/// </summary>
public interface IUpdateBuilderOutputInto : IUpdateBuilderSet
{
    /// <summary>
    /// Appends <c>INTO table (col, ...)</c> (SQL Server), redirecting the
    /// <c>OUTPUT</c> rows into <paramref name="table"/> instead of returning them
    /// to the caller.
    /// </summary>
    /// <param name="table">The table the output rows are inserted into.</param>
    /// <param name="columns">The columns to populate, in output order; omit to target the table's columns positionally.</param>
    /// <returns>The builder positioned to continue with <c>WHERE</c> / <c>FROM</c> / build.</returns>
    IUpdateBuilderSet Into(DbTableBase table, params DbColumn[] columns);
}
