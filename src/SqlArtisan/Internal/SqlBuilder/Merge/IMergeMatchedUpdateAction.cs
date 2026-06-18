namespace SqlArtisan.Internal;

/// <summary>
/// Follows a <c>WHEN MATCHED ... UPDATE SET</c>. Either continue with another
/// branch / <c>Build</c> (inherited), or append Oracle's in-clause
/// <c>DELETE WHERE condition</c> to remove the just-updated rows that match it.
/// </summary>
public interface IMergeMatchedUpdateAction : IMergeBuilderWhen
{
    IMergeBuilderWhen DeleteWhere(SqlCondition condition);
}
