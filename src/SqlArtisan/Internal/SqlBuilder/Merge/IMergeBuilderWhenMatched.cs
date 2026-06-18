namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>WHEN MATCHED [AND ...] THEN</c>: update the matched rows
/// (<c>UPDATE SET ...</c>) or remove them (<c>DELETE</c>, SQL Server).
/// </summary>
public interface IMergeBuilderWhenMatched
{
    IMergeBuilderThenUpdateSet ThenUpdateSet(params EqualityBasedCondition[] assignments);

    IMergeBuilderOn ThenDelete();
}
