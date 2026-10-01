namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>WHEN MATCHED AND condition THEN</c>: update the matched rows
/// (<c>UPDATE SET ...</c>) or remove them (<c>DELETE</c>).
/// </summary>
public interface IMergeBuilderWhenMatchedAnd
{
    /// <summary>
    /// Appends <c>THEN DELETE</c>, removing the matched rows.
    /// </summary>
    /// <returns>The builder positioned to chain another <c>WHEN</c> branch or build.</returns>
    /// <remarks>PostgreSQL (15+) and SQL Server syntax.</remarks>
    IMergeBuilderWhen ThenDelete();

    /// <summary>
    /// Appends <c>THEN UPDATE SET col = value, ...</c>, updating the matched rows.
    /// </summary>
    /// <param name="assignments">The <c>column == value</c> updates; values are typically source columns and literals are auto-parameterized.</param>
    /// <returns>The builder positioned to chain another <c>WHEN</c> branch or build.</returns>
    /// <remarks>Oracle, PostgreSQL (15+), and SQL Server syntax for the action; the
    /// conditioned branch is PostgreSQL's and SQL Server's.</remarks>
    IMergeBuilderWhen ThenUpdateSet(params EqualityCondition[] assignments);
}
