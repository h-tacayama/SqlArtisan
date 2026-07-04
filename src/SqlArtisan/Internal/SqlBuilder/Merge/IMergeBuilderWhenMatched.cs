namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>WHEN MATCHED [AND ...] THEN</c>: update the matched rows
/// (<c>UPDATE SET ...</c>) or remove them (<c>DELETE</c>, PostgreSQL 15+ / SQL Server).
/// </summary>
public interface IMergeBuilderWhenMatched
{
    /// <summary>
    /// Appends <c>THEN UPDATE SET col = value, ...</c>, updating the matched rows.
    /// </summary>
    /// <param name="assignments">The <c>column == value</c> updates; values are typically source columns and literals are auto-parameterized.</param>
    /// <returns>The builder positioned to chain another branch, append Oracle's <c>DELETE WHERE</c>, or build.</returns>
    IMergeBuilderThenUpdateSet ThenUpdateSet(params EqualityBasedCondition[] assignments);

    /// <summary>
    /// Appends <c>THEN DELETE</c>, removing the matched rows (PostgreSQL 15+ / SQL Server).
    /// </summary>
    /// <returns>The builder positioned to chain another <c>WHEN</c> branch or build.</returns>
    IMergeBuilderOn ThenDelete();
}
