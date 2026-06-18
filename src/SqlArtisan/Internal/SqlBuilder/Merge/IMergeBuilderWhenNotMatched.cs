namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>WHEN NOT MATCHED [AND ...] THEN</c>: insert a new row. Name
/// the target columns, then supply their values via
/// <see cref="IMergeBuilderThenInsert"/>.
/// </summary>
public interface IMergeBuilderWhenNotMatched
{
    IMergeBuilderThenInsert ThenInsert(params DbColumn[] columns);
}
