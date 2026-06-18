namespace SqlArtisan.Internal;

/// <summary>
/// The action for a <c>WHEN MATCHED</c> branch: update the matched rows
/// (<c>UPDATE SET ...</c>) or remove them (<c>DELETE</c>, SQL Server).
/// </summary>
public interface IMergeMatchedThen
{
    IMergeMatchedUpdateAction ThenUpdateSet(params EqualityBasedCondition[] assignments);

    IMergeBuilderWhen ThenDelete();
}
