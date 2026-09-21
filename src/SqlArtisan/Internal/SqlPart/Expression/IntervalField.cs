namespace SqlArtisan.Internal;

public sealed class IntervalField : SqlPart
{
    private readonly string? _precision;

    internal IntervalField(DateTimePart field, int? precision)
    {
        if (precision is int p)
        {
            IntervalFieldGuard.ValidatePrecision(p, field);
        }

        Field = field;
        _precision = precision?.ToInvariantString();
    }

    internal DateTimePart Field { get; }

    internal bool HasPrecision => _precision is not null;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(DatepartKeywords.Of(Field));

        if (_precision is { } precision)
        {
            buffer.Append('(').Append(precision).Append(')');
        }
    }
}
