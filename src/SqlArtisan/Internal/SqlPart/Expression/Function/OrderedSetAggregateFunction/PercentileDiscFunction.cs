namespace SqlArtisan.Internal;

/// <summary>
/// The <c>PERCENTILE_DISC</c> ordered-set aggregate, pending its mandatory
/// <c>WITHIN GROUP (ORDER BY ...)</c> clause.
/// </summary>
public sealed class PercentileDiscFunction : IIncompleteExpression
{
    private readonly double _fraction;

    internal PercentileDiscFunction(double fraction) =>
        _fraction = PercentileFractionGuard.Validate(fraction);

    string IIncompleteExpression.CompletionHint =>
        "Complete it with .WithinGroup(OrderBy(...)) — PERCENTILE_DISC requires a WITHIN GROUP clause.";

    /// <summary>
    /// Supplies the mandatory <c>WITHIN GROUP (ORDER BY ...)</c> clause that the
    /// percentile is computed over.
    /// </summary>
    public PercentileFunction WithinGroup(OrderByClause orderByClause) =>
        new(Keywords.PercentileDisc, _fraction, new WithinGroupClause(orderByClause));
}
