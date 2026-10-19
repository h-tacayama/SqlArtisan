namespace SqlArtisan.Internal;

/// <summary>
/// MySQL's <c>INTERVAL n unit</c>, valid only beside a date in <c>+</c> / <c>-</c> or as
/// <c>DATE_ADD</c> / <c>DATE_SUB</c>'s interval, so it is no <see cref="SqlExpression"/>
/// (#666).
/// </summary>
public sealed class IntervalExpression : SqlPart, IIncompleteExpression
{
    private readonly SqlExpression _quantity;
    private readonly string _unit;

    internal IntervalExpression(SqlExpression quantity, DateTimePart unit)
    {
        _quantity = quantity;
        _unit = DatepartKeywords.Resolve(unit, Keywords.Interval, nameof(unit));
    }

    string IIncompleteExpression.IncompleteMessage =>
        "INTERVAL requires a date; add it to or subtract it from one, as in "
            + "col + Interval(...), or pass it to DateAdd or DateSub.";

    /// <summary>
    /// The interval-first date addition MySQL also reads:
    /// <c>INTERVAL n unit + <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The interval.</param>
    /// <param name="rightSide">The date added to.</param>
    /// <returns>The addition expression.</returns>
    public static AdditionOperator operator +(IntervalExpression @this, object rightSide) =>
        new(
            new PositionBoundOperand(OperandGuard.ThrowIfNull(@this, Operators.Plus)),
            ExpressionResolver.Resolve(rightSide));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Interval)
        .PrependSpace(_quantity)
        .AppendSpace()
        .Append(_unit);
}
