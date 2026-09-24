namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>WHEN MATCHED ... UPDATE SET</c>: continue with another
/// branch or <c>Build</c> (inherited), filter the update with Oracle's <c>WHERE</c>,
/// or append Oracle's in-clause <c>DELETE WHERE condition</c>.
/// </summary>
public interface IMergeBuilderThenUpdateSet : IMergeBuilderWhen
{
    /// <summary>
    /// Appends Oracle's in-clause <c>DELETE WHERE condition</c>, removing the just-updated rows
    /// that satisfy it.
    /// </summary>
    /// <param name="condition">The predicate selecting which updated rows to delete; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned to chain another <c>WHEN</c> branch or build.</returns>
    IMergeBuilderWhen DeleteWhere(SqlCondition condition);

    /// <summary>
    /// Appends Oracle's <c>WHERE condition</c> to the <c>UPDATE SET</c>, updating only the matched
    /// rows that satisfy it.
    /// </summary>
    /// <param name="condition">The predicate over the target and source rows; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned to append <c>DELETE WHERE</c>, chain another <c>WHEN</c> branch, or build.</returns>
    /// <remarks>
    /// Oracle syntax; PostgreSQL and SQL Server spell the filter
    /// <c>WhenMatched(condition)</c>.
    /// </remarks>
    IMergeBuilderUpdateWhere UpdateWhere(SqlCondition condition);
}
