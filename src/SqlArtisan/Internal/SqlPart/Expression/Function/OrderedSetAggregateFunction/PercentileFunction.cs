namespace SqlArtisan.Internal;

/// <summary>
/// An ordered-set aggregate (<c>PERCENTILE_CONT</c> / <c>PERCENTILE_DISC</c>)
/// in the form <c>FUNC(fraction) WITHIN GROUP (ORDER BY ...)</c>. Attach
/// <see cref="Over()"/> or <see cref="Over(PartitionByClause)"/> for the
/// windowed form.
/// </summary>
public sealed class PercentileFunction : SqlExpression
{
    private readonly string _function;
    private readonly double _fraction;
    private readonly WithinGroupClause _withinGroupClause;

    internal PercentileFunction(
        string function,
        double fraction,
        WithinGroupClause withinGroupClause)
    {
        _function = function;
        _fraction = fraction;
        _withinGroupClause = withinGroupClause;
    }

    /// <summary>
    /// Turns the ordered-set aggregate into a window function over the whole
    /// result set: <c>... OVER ()</c>.
    /// </summary>
    public WindowFunction Over() => new(this, OverClause.Of());

    /// <summary>
    /// Turns the ordered-set aggregate into a window function partitioned by the
    /// given expressions: <c>... OVER (PARTITION BY ...)</c>.
    /// </summary>
    public WindowFunction Over(PartitionByClause partitionByClause) =>
        new(this, OverClause.Of(partitionByClause));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_function)
        .OpenParenthesis()
        .Append(_fraction.ToInvariantString())
        .CloseParenthesis()
        .PrependSpace(_withinGroupClause);
}
