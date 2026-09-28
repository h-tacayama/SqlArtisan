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
        _fraction = PercentileFractionGuard.Validate(fraction);
    }

    string IIncompleteExpression.IncompleteMessage =>
        "PERCENTILE_CONT requires a WITHIN GROUP clause; complete it with "
            + ".WithinGroup(OrderBy(...)).";

    /// <summary>
    /// Supplies the mandatory <c>WITHIN GROUP (ORDER BY ...)</c> clause that the
    /// percentile is computed over.
    /// </summary>
    /// <remarks>Oracle, PostgreSQL, and SQL Server syntax.</remarks>
    public PercentileFunction WithinGroup(OrderByClause orderByClause) =>
        new(Keywords.PercentileCont, _fraction, new WithinGroupClause(orderByClause));
}
