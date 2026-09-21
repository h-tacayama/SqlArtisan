namespace SqlArtisan.Internal;

/// <summary>
/// The state after a SQL Server <c>WHEN NOT MATCHED BY SOURCE [AND ...] THEN</c>:
/// update the unmatched target rows (<c>UPDATE SET ...</c>) or remove them
/// (<c>DELETE</c>).
/// </summary>
public interface IMergeBuilderWhenNotMatchedBySource
{
    /// <summary>
    /// Appends <c>THEN DELETE</c>, removing the unmatched target rows.
    /// </summary>
    /// <returns>The builder positioned to chain another <c>WHEN</c> branch or build.</returns>
    /// <remarks>PostgreSQL (15+) and SQL Server syntax for the action; the branch itself
    /// is SQL Server's alone.</remarks>
    IMergeBuilderWhen ThenDelete();

    /// <summary>
    /// Appends <c>THEN UPDATE SET col = value, ...</c>, updating the unmatched target rows.
    /// </summary>
    /// <param name="assignments">The <c>column == value</c> updates; literals are auto-parameterized.</param>
    /// <returns>The builder positioned to chain another <c>WHEN</c> branch or build.</returns>
    /// <remarks>Oracle, PostgreSQL (15+), and SQL Server syntax for the action; the branch
    /// itself is SQL Server's alone.</remarks>
    IMergeBuilderWhen ThenUpdateSet(params EqualityCondition[] assignments);
}
