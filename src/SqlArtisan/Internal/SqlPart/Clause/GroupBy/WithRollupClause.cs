namespace SqlArtisan.Internal;

// Renders MySQL's `WITH ROLLUP` GROUP BY suffix. The user-facing contract lives
// on ISelectBuilderGroupBy.WithRollup(); this is the clause it appends.
internal sealed class WithRollupClause : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append($"{Keywords.With} {Keywords.Rollup}");
}
