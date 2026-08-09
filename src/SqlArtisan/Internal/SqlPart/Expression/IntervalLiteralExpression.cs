namespace SqlArtisan.Internal;

public sealed class IntervalLiteralExpression : SqlExpression
{
    private readonly string _value;
    private readonly IntervalField? _field;
    private readonly IntervalField? _trailingField;

    internal IntervalLiteralExpression(
        string value,
        IntervalField? field = null,
        IntervalField? trailingField = null)
    {
        StringGuard.ThrowIfNullOrEmpty(value, "INTERVAL requires a literal value.");

        if (field is not null && trailingField is not null)
        {
            IntervalFieldGuard.ValidateRange(field, trailingField);
        }
        else if (field is not null)
        {
            IntervalFieldGuard.ValidateSoleField(field);
        }

        _value = value;
        _field = field;
        _trailingField = trailingField;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Interval)
            .AppendSpace()
            .AppendStringLiteral(_value);

        if (_field is IntervalField field)
        {
            buffer.AppendSpace()
                .Append(field);

            if (_trailingField is IntervalField trailingField)
            {
                buffer.AppendSpace()
                    .Append(Keywords.To)
                    .AppendSpace()
                    .Append(trailingField);
            }
        }
    }
}
