namespace SqlArtisan.Internal;

/// <summary>
/// The <c>PERCENTILE_CONT</c> ordered-set aggregate, pending its mandatory
/// <c>WITHIN GROUP (ORDER BY ...)</c> clause.
/// </summary>
public sealed class PercentileContFunction : IIncompleteExpression
{
    private readonly double _fraction;

    internal PercentileContFunction(double fraction) =>
        _fraction = PercentileFractionGuard.Validate(fraction);

    string IIncompleteExpression.CompletionHint =>
        "Complete it with .WithinGroup(OrderBy(...)) — PERCENTILE_CONT requires a WITHIN GROUP clause.";

    /// <summary>
    /// Supplies the mandatory <c>WITHIN GROUP (ORDER BY ...)</c> clause that the
    /// percentile is computed over.
    /// </summary>
    public PercentileFunction WithinGroup(OrderByClause orderByClause) =>
        new(Keywords.PercentileCont, _fraction, new WithinGroupClause(orderByClause));
}
