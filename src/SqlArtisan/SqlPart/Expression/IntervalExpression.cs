using SqlArtisan.Internal;

namespace SqlArtisan;

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
    /// The interval-first date addition, which MySQL 8.0 also reads:
    /// <c>INTERVAL n unit + <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The interval.</param>
    /// <param name="rightSide">The date added to.</param>
    /// <returns>The addition expression.</returns>
    public static SqlExpression operator +(IntervalExpression @this, object rightSide) =>
        new AdditionOperator(
            new PositionBoundOperand(OperandGuard.ThrowIfNull(@this, Operators.Plus)),
            ExpressionResolver.Resolve(rightSide));

    /// <summary>
    /// The string-first date addition:
    /// <c><paramref name="leftSide"/> + INTERVAL n unit</c>, the string bound as a parameter.
    /// </summary>
    /// <param name="leftSide">The date, bound as a parameter.</param>
    /// <param name="rightSide">The interval.</param>
    /// <returns>The addition expression.</returns>
    public static SqlExpression operator +(string leftSide, IntervalExpression rightSide) =>
        new AdditionOperator(
            ExpressionResolver.Resolve(OperandGuard.ThrowIfNull(leftSide, Operators.Plus)),
            ExpressionResolver.ResolveIntervalOperand(rightSide));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Interval)
        .PrependSpace(_quantity)
        .AppendSpace()
        .Append(_unit);
}
