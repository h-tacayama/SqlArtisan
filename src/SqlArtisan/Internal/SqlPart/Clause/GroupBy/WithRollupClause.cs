namespace SqlArtisan.Internal;

// The user-facing contract, including which dialects accept this suffix, lives
// on ISelectBuilderGroupBy.WithRollup(); this is the clause it appends.
internal sealed class WithRollupClause : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append($"{Keywords.With} {Keywords.Rollup}");
}
