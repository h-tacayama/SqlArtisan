namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>WHEN MATCHED ... UPDATE SET</c>: continue with another
/// branch or <c>Build</c> (inherited), or append Oracle's in-clause
/// <c>DELETE WHERE condition</c> to remove the just-updated rows that match it.
/// </summary>
public interface IMergeBuilderThenUpdateSet : IMergeBuilderOn
{
    /// <summary>
    /// Appends Oracle's in-clause <c>DELETE WHERE condition</c>, removing the just-updated rows that satisfy it.
    /// </summary>
    /// <param name="condition">The predicate selecting which updated rows to delete; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned to chain another <c>WHEN</c> branch or build.</returns>
    IMergeBuilderOn DeleteWhere(SqlCondition condition);
}
