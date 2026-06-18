namespace SqlArtisan.Internal;

/// <summary>
/// The state after a SQL Server <c>WHEN NOT MATCHED BY SOURCE [AND ...] THEN</c>:
/// update the unmatched target rows (<c>UPDATE SET ...</c>) or remove them
/// (<c>DELETE</c>).
/// </summary>
public interface IMergeBuilderWhenNotMatchedBySource
{
    IMergeBuilderOn ThenUpdateSet(params EqualityBasedCondition[] assignments);

    IMergeBuilderOn ThenDelete();
}
