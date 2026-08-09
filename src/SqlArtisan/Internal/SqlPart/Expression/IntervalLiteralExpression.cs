namespace SqlArtisan.Internal;

public sealed class IntervalLiteralExpression : SqlExpression
{
    private readonly string _value;
    private readonly DateTimePart? _field;
    private readonly DateTimePart? _trailingField;

    internal IntervalLiteralExpression(
        string value,
        DateTimePart? field = null,
        DateTimePart? trailingField = null)
    {
        StringGuard.ThrowIfNullOrEmpty(value, "INTERVAL requires a literal value.");

        _value = value;
        _field = field;
        _trailingField = trailingField;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Interval)
            .AppendSpace()
            .AppendStringLiteral(_value);

        if (_field is DateTimePart field)
        {
            buffer.AppendSpace()
                .Append(DatepartKeywords.Of(field));

            if (_trailingField is DateTimePart trailingField)
            {
                buffer.AppendSpace()
                    .Append(Keywords.To)
                    .AppendSpace()
                    .Append(DatepartKeywords.Of(trailingField));
            }
        }
    }
}
