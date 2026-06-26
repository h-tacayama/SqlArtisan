namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>ON (...)</c>: add one or more <c>WHEN</c> branches, then
/// <see cref="ISqlBuilder.Build()">Build</see>. The branches are per-dialect:
/// <c>WHEN NOT MATCHED BY SOURCE</c> is SQL Server only.
/// </summary>
public interface IMergeBuilderOn : ISqlBuilder
{
    /// <summary>
    /// <c>WHEN MATCHED THEN</c>: act on rows that exist in both target and source.
    /// </summary>
    /// <returns>The builder positioned to supply the matched action (<c>UPDATE</c> or <c>DELETE</c>).</returns>
    IMergeBuilderWhenMatched WhenMatched();

    /// <summary>
    /// <c>WHEN MATCHED AND condition THEN</c>: as <see cref="WhenMatched()"/>, but
    /// only for matched rows that also satisfy <paramref name="extraCondition"/>.
    /// </summary>
    /// <param name="extraCondition">The extra predicate the matched rows must satisfy.</param>
    /// <returns>The builder positioned to supply the matched action (<c>UPDATE</c> or <c>DELETE</c>).</returns>
    IMergeBuilderWhenMatched WhenMatched(SqlCondition extraCondition);

    /// <summary>
    /// <c>WHEN NOT MATCHED THEN</c>: act on source rows with no target match
    /// (typically an <c>INSERT</c>).
    /// </summary>
    /// <returns>The builder positioned to supply the not-matched action (typically <c>INSERT</c>).</returns>
    IMergeBuilderWhenNotMatched WhenNotMatched();

    /// <summary>
    /// <c>WHEN NOT MATCHED AND condition THEN</c>: as <see cref="WhenNotMatched()"/>,
    /// but only for source rows that also satisfy <paramref name="extraCondition"/>.
    /// </summary>
    /// <param name="extraCondition">The extra predicate the unmatched source rows must satisfy.</param>
    /// <returns>The builder positioned to supply the not-matched action (typically <c>INSERT</c>).</returns>
    IMergeBuilderWhenNotMatched WhenNotMatched(SqlCondition extraCondition);

    /// <summary>
    /// <c>WHEN NOT MATCHED BY SOURCE THEN</c> (SQL Server): act on target rows with
    /// no source match (typically an <c>UPDATE</c> or <c>DELETE</c>).
    /// </summary>
    /// <returns>The builder positioned to supply the not-matched-by-source action (<c>UPDATE</c> or <c>DELETE</c>).</returns>
    IMergeBuilderWhenNotMatchedBySource WhenNotMatchedBySource();

    /// <summary>
    /// <c>WHEN NOT MATCHED BY SOURCE AND condition THEN</c> (SQL Server): as
    /// <see cref="WhenNotMatchedBySource()"/>, but filtered by
    /// <paramref name="extraCondition"/>.
    /// </summary>
    /// <param name="extraCondition">The extra predicate the unmatched target rows must satisfy.</param>
    /// <returns>The builder positioned to supply the not-matched-by-source action (<c>UPDATE</c> or <c>DELETE</c>).</returns>
    IMergeBuilderWhenNotMatchedBySource WhenNotMatchedBySource(SqlCondition extraCondition);
}
