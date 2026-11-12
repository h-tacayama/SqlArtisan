using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An ordered-set aggregate (<c>PERCENTILE_CONT</c> / <c>PERCENTILE_DISC</c>)
/// in the form <c>FUNC(fraction) WITHIN GROUP (ORDER BY ...)</c>. Attach
/// <see cref="Over()"/> or <see cref="Over(PartitionByClause)"/> for the
/// windowed form.
/// </summary>
public sealed class PercentileFunction : SqlExpression
{
    private readonly string _functionName;

    private readonly string _fraction;
    private readonly WithinGroupClause _withinGroupClause;

    internal PercentileFunction(
        string functionName,
        double fraction,
        WithinGroupClause withinGroupClause)
    {
        _functionName = functionName;
        _fraction = fraction.ToInvariantString();
        _withinGroupClause = withinGroupClause;
    }

    /// <summary>
    /// Turns the ordered-set aggregate into a window function over the whole
    /// result set: <c>... OVER ()</c>.
    /// </summary>
    public SqlExpression Over() => new WindowFunction(this, OverClause.Of());

    /// <summary>
    /// Turns the ordered-set aggregate into a window function partitioned by the
    /// given expressions: <c>... OVER (PARTITION BY ...)</c>.
    /// </summary>
    public SqlExpression Over(PartitionByClause partitionByClause) =>
        new WindowFunction(this, OverClause.Of(partitionByClause));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_functionName)
        .OpenParenthesis()
        .Append(_fraction)
        .CloseParenthesis()
        .PrependSpace(_withinGroupClause);
}
