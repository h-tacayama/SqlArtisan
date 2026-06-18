namespace SqlArtisan.Internal;

/// <summary>
/// The MERGE clause hub: add one or more <c>WHEN</c> branches, then
/// <see cref="ISqlBuilder.Build()">Build</see>. Each branch's terminal action
/// returns here so further branches can be chained. The branches are per-dialect
/// by design — <c>WHEN NOT MATCHED BY SOURCE</c> is SQL Server only.
/// </summary>
public interface IMergeBuilderWhen : ISqlBuilder
{
    /// <summary>
    /// <c>WHEN MATCHED THEN</c>: act on rows that exist in both target and source.
    /// </summary>
    IMergeMatchedThen WhenMatched();

    /// <summary>
    /// <c>WHEN MATCHED AND condition THEN</c>: as <see cref="WhenMatched()"/>, but
    /// only for matched rows that also satisfy <paramref name="extraCondition"/>.
    /// </summary>
    IMergeMatchedThen WhenMatched(SqlCondition extraCondition);

    /// <summary>
    /// <c>WHEN NOT MATCHED THEN</c>: act on source rows with no target match
    /// (typically an <c>INSERT</c>).
    /// </summary>
    IMergeNotMatchedThen WhenNotMatched();

    /// <summary>
    /// <c>WHEN NOT MATCHED AND condition THEN</c>: as <see cref="WhenNotMatched()"/>,
    /// but only for source rows that also satisfy <paramref name="extraCondition"/>.
    /// </summary>
    IMergeNotMatchedThen WhenNotMatched(SqlCondition extraCondition);

    /// <summary>
    /// <c>WHEN NOT MATCHED BY SOURCE THEN</c> (SQL Server): act on target rows with
    /// no source match (typically an <c>UPDATE</c> or <c>DELETE</c>).
    /// </summary>
    IMergeNotMatchedBySourceThen WhenNotMatchedBySource();

    /// <summary>
    /// <c>WHEN NOT MATCHED BY SOURCE AND condition THEN</c> (SQL Server): as
    /// <see cref="WhenNotMatchedBySource()"/>, but filtered by
    /// <paramref name="extraCondition"/>.
    /// </summary>
    IMergeNotMatchedBySourceThen WhenNotMatchedBySource(SqlCondition extraCondition);
}
