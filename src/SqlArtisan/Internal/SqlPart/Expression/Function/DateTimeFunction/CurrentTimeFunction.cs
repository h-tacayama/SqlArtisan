namespace SqlArtisan.Internal;

internal sealed class CurrentTimeFunction : SqlExpression
{
    private readonly string? _precision;

    internal CurrentTimeFunction(int? precision)
    {
        // The same domain as CurrentTimestampFunction's precision.
        if (precision < 0)
        {
            throw new ArgumentException("CURRENT_TIME precision must not be negative.");
        }

        _precision = precision?.ToInvariantString();
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.CurrentTime);

        if (_precision is { } precision)
        {
            buffer.Append('(').Append(precision).Append(')');
        }
    }
}
