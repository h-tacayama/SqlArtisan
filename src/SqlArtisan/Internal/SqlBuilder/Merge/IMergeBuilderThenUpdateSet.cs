namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>WHEN MATCHED ... UPDATE SET</c>: continue with another
/// branch or <c>Build</c> (inherited), or append Oracle's in-clause
/// <c>DELETE WHERE condition</c> to remove the just-updated rows that match it.
/// </summary>
public interface IMergeBuilderThenUpdateSet : IMergeBuilderOn
{
    IMergeBuilderOn DeleteWhere(SqlCondition condition);
}
