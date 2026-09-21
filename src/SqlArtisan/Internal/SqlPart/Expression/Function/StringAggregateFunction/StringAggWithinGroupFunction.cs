namespace SqlArtisan.Internal;

/// <summary>
/// A <c>STRING_AGG(expr, separator) WITHIN GROUP (ORDER BY ...)</c> string
/// aggregate (SQL Server), produced by
/// <see cref="StringAggFunction.WithinGroup(OrderByClause)"/>.
/// </summary>
// A separate node rather than a mutable field on StringAggFunction: a held
// call handle then stays reusable, and the completed shape cannot be
// re-completed (the same immutability the other completing calls keep).
public sealed class StringAggWithinGroupFunction : SqlExpression
{
    private readonly StringAggFunction _call;
    private readonly WithinGroupClause _withinGroupClause;

    internal StringAggWithinGroupFunction(
        StringAggFunction call,
        WithinGroupClause withinGroupClause)
    {
        _call = call;
        _withinGroupClause = withinGroupClause;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_call)
        .AppendSpace()
        .Append(_withinGroupClause);
}
