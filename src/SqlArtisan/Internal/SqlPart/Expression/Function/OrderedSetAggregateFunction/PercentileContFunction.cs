namespace SqlArtisan.Internal;

/// <summary>
/// The <c>PERCENTILE_CONT</c> ordered-set aggregate, pending its mandatory
/// <c>WITHIN GROUP (ORDER BY ...)</c> clause.
/// </summary>
public sealed class PercentileContFunction : IIncompleteExpression
{
    private readonly double _fraction;

    internal PercentileContFunction(double fraction)
    {
        if (!double.IsFinite(fraction))
        {
            throw new ArgumentException(
                "The percentile fraction must be a finite number.",
                nameof(fraction));
        }

        _fraction = fraction;
    }

    string IIncompleteExpression.CompletionHint =>
        "Complete it with .WithinGroup(OrderBy(...)) — PERCENTILE_CONT requires a WITHIN GROUP clause.";

    /// <summary>
    /// Supplies the mandatory <c>WITHIN GROUP (ORDER BY ...)</c> clause that the
    /// percentile is computed over.
    /// </summary>
    public PercentileFunction WithinGroup(OrderByClause orderByClause) =>
        new(Keywords.PercentileCont, _fraction, new WithinGroupClause(orderByClause));
}
