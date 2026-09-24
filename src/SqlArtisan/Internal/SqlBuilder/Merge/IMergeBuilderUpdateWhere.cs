namespace SqlArtisan.Internal;

/// <summary>
/// The state after Oracle's <c>UPDATE SET ... WHERE</c>: continue with another branch or
/// <c>Build</c> (inherited), or append <c>DELETE WHERE condition</c>, which reaches only the
/// rows the <c>WHERE</c> let through.
/// </summary>
public interface IMergeBuilderUpdateWhere : IMergeBuilderWhen
{
    /// <summary>
    /// Appends Oracle's in-clause <c>DELETE WHERE condition</c>, removing the just-updated rows
    /// that satisfy it.
    /// </summary>
    /// <param name="condition">The predicate selecting which updated rows to delete; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned to chain another <c>WHEN</c> branch or build.</returns>
    IMergeBuilderWhen DeleteWhere(SqlCondition condition);
}
