namespace SqlArtisan.Internal;

public sealed class IntervalExpression : SqlExpression
{
    private readonly SqlExpression _quantity;
    private readonly string _unit;

    internal IntervalExpression(SqlExpression quantity, DateTimePart unit)
    {
        _quantity = quantity;
        _unit = DatepartKeywords.Resolve(unit, Keywords.Interval, nameof(unit));
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Interval)
        .PrependSpace(_quantity)
        .AppendSpace()
        .Append(_unit);
}
