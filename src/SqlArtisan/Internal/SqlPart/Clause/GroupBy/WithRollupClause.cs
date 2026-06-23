namespace SqlArtisan.Internal;

/// <summary>
/// The MySQL <c>WITH ROLLUP</c> suffix on a <c>GROUP BY</c> clause, added with
/// <c>.GroupBy(...).WithRollup()</c>. It renders <c>WITH ROLLUP</c> immediately
/// after the grouping list (<c>GROUP BY a, b WITH ROLLUP</c>). Only MySQL accepts
/// this form; elsewhere the standard <c>Sql.Rollup(...)</c> function form is the
/// portable spelling.
/// </summary>
internal sealed class WithRollupClause : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append($"{Keywords.With} {Keywords.Rollup}");
}
