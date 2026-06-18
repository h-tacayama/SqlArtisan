namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>ON (...)</c>: add one or more <c>WHEN</c> branches, then
/// <see cref="ISqlBuilder.Build()">Build</see>. Each branch's terminal action
/// returns here so further branches can be chained. The branches are per-dialect
/// by design — <c>WHEN NOT MATCHED BY SOURCE</c> is SQL Server only.
/// </summary>
public interface IMergeBuilderOn : ISqlBuilder
{
    /// <summary>
    /// <c>WHEN MATCHED THEN</c>: act on rows that exist in both target and source.
    /// </summary>
    IMergeBuilderWhenMatched WhenMatched();

    /// <summary>
    /// <c>WHEN MATCHED AND condition THEN</c>: as <see cref="WhenMatched()"/>, but
    /// only for matched rows that also satisfy <paramref name="extraCondition"/>.
    /// </summary>
    IMergeBuilderWhenMatched WhenMatched(SqlCondition extraCondition);

    /// <summary>
    /// <c>WHEN NOT MATCHED THEN</c>: act on source rows with no target match
    /// (typically an <c>INSERT</c>).
    /// </summary>
    IMergeBuilderWhenNotMatched WhenNotMatched();

    /// <summary>
    /// <c>WHEN NOT MATCHED AND condition THEN</c>: as <see cref="WhenNotMatched()"/>,
    /// but only for source rows that also satisfy <paramref name="extraCondition"/>.
    /// </summary>
    IMergeBuilderWhenNotMatched WhenNotMatched(SqlCondition extraCondition);

    /// <summary>
    /// <c>WHEN NOT MATCHED BY SOURCE THEN</c> (SQL Server): act on target rows with
    /// no source match (typically an <c>UPDATE</c> or <c>DELETE</c>).
    /// </summary>
    IMergeBuilderWhenNotMatchedBySource WhenNotMatchedBySource();

    /// <summary>
    /// <c>WHEN NOT MATCHED BY SOURCE AND condition THEN</c> (SQL Server): as
    /// <see cref="WhenNotMatchedBySource()"/>, but filtered by
    /// <paramref name="extraCondition"/>.
    /// </summary>
    IMergeBuilderWhenNotMatchedBySource WhenNotMatchedBySource(SqlCondition extraCondition);
}
