namespace SqlArtisan.Internal;

public sealed class CurrentTimestampFunction : SqlExpression
{
    private readonly string? _precision;

    internal CurrentTimestampFunction(int? precision)
    {
        // 0-9 spans every dialect's range (Oracle's 9 is the widest), so a value
        // outside it is rejected everywhere and fails eagerly (ADR 0012).
        if (precision is < 0 or > 9)
        {
            throw new ArgumentException("CURRENT_TIMESTAMP precision must be between 0 and 9.");
        }

        _precision = precision?.ToInvariantString();
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.CurrentTimestamp);

        if (_precision is { } precision)
        {
            buffer.Append('(').Append(precision).Append(')');
        }
    }
}
