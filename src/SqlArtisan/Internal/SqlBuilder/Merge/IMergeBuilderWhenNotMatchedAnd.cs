namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>WHEN NOT MATCHED AND condition THEN</c>: insert a new row, with
/// a column list or positionally, then supply its values via
/// <see cref="IMergeBuilderWhenNotMatchedAndThenInsert"/>.
/// </summary>
public interface IMergeBuilderWhenNotMatchedAnd
{
    /// <summary>
    /// Appends the positional <c>THEN INSERT</c> (no column list): the following
    /// <c>Values(...)</c> supplies one value per target-table column, in table order.
    /// </summary>
    /// <returns>The builder positioned to supply the <c>VALUES (...)</c> list.</returns>
    /// <remarks>Oracle, PostgreSQL (15+), and SQL Server syntax for the action; the
    /// conditioned branch is PostgreSQL's and SQL Server's.</remarks>
    IMergeBuilderWhenNotMatchedAndThenInsert ThenInsert();

    /// <summary>
    /// Appends <c>THEN INSERT (col, ...)</c>, naming the target columns to populate; supply their
    /// values next with <c>Values(...)</c>.
    /// </summary>
    /// <param name="columns">The target columns, emitted in parentheses; must be non-empty.</param>
    /// <returns>The builder positioned to supply the <c>VALUES (...)</c> list.</returns>
    /// <remarks>Oracle, PostgreSQL (15+), and SQL Server syntax for the action; the
    /// conditioned branch is PostgreSQL's and SQL Server's.</remarks>
    IMergeBuilderWhenNotMatchedAndThenInsert ThenInsert(params DbColumn[] columns);
}
