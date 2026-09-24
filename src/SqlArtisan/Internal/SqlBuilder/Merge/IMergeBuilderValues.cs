namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>WHEN NOT MATCHED ... INSERT ... VALUES (...)</c>: continue with
/// another branch or <c>Build</c> (inherited), or filter the insert with Oracle's
/// <c>WHERE</c>.
/// </summary>
public interface IMergeBuilderValues : IMergeBuilderWhen
{
    /// <summary>
    /// Appends Oracle's <c>WHERE condition</c> to the <c>INSERT</c>, inserting only the unmatched
    /// source rows that satisfy it.
    /// </summary>
    /// <param name="condition">The predicate over the source row; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned to chain another <c>WHEN</c> branch or build.</returns>
    /// <remarks>
    /// Oracle syntax. The condition may name only source columns: an unmatched row has no
    /// target row, and Oracle rejects a target column here.
    /// </remarks>
    IMergeBuilderWhen InsertWhere(SqlCondition condition);
}
