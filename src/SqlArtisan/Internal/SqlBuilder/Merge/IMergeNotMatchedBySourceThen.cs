namespace SqlArtisan.Internal;

/// <summary>
/// The action for a SQL Server <c>WHEN NOT MATCHED BY SOURCE</c> branch: update
/// the unmatched target rows (<c>UPDATE SET ...</c>) or remove them (<c>DELETE</c>).
/// </summary>
public interface IMergeNotMatchedBySourceThen
{
    IMergeBuilderWhen ThenUpdateSet(params EqualityBasedCondition[] assignments);

    IMergeBuilderWhen ThenDelete();
}
