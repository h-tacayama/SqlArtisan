namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT INTO table OUTPUT ...</c> (SQL Server, no column list):
/// optionally redirect the output <c>INTO</c> a table, then supply the rows.
/// </summary>
/// <remarks>
/// <c>Set(...)</c> is absent here, unlike before the <c>OUTPUT</c>: it emits the column list,
/// which T-SQL places ahead of <c>OUTPUT</c>, so the pair has no spelling in this order.
/// </remarks>
public interface IInsertBuilderTableOutputInto : IInsertBuilderTableOutputRows
{
    /// <summary>
    /// Appends <c>INTO table (col, ...)</c> (SQL Server), redirecting the
    /// <c>OUTPUT</c> rows into <paramref name="table"/> instead of returning them
    /// to the caller.
    /// </summary>
    /// <param name="table">The table the output rows are inserted into.</param>
    /// <param name="columns">The columns to populate, in output order; omit to target the table's columns positionally.</param>
    /// <returns>The builder positioned to supply the rows with <c>Values(...)</c>.</returns>
    IInsertBuilderTableOutputRows Into(DbTableBase table, params DbColumn[] columns);
}
