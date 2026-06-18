namespace SqlArtisan.Internal;

/// <summary>
/// The action for a <c>WHEN NOT MATCHED</c> branch: insert a new row. Name the
/// target columns, then supply their values via <see cref="IMergeInsertValues"/>.
/// </summary>
public interface IMergeNotMatchedThen
{
    IMergeInsertValues ThenInsert(params DbColumn[] columns);
}
