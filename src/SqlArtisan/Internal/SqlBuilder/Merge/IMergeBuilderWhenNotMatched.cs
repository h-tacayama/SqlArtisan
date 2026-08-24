namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>WHEN NOT MATCHED [AND ...] THEN</c>: insert a new row. Name
/// the target columns, then supply their values via
/// <see cref="IMergeBuilderThenInsert"/>.
/// </summary>
public interface IMergeBuilderWhenNotMatched
{
    /// <summary>
    /// Appends the positional <c>THEN INSERT</c> (no column list): the following
    /// <c>Values(...)</c> supplies one value per target-table column, in table order.
    /// </summary>
    /// <returns>The builder positioned to supply the <c>VALUES (...)</c> list.</returns>
    IMergeBuilderThenInsert ThenInsert();

    /// <summary>
    /// Appends <c>THEN INSERT (col, ...)</c>, naming the target columns to populate; supply their values next with <c>Values(...)</c>.
    /// </summary>
    /// <param name="columns">The target columns, emitted in parentheses; must be non-empty.</param>
    /// <returns>The builder positioned to supply the <c>VALUES (...)</c> list.</returns>
    IMergeBuilderThenInsert ThenInsert(params DbColumn[] columns);
}
