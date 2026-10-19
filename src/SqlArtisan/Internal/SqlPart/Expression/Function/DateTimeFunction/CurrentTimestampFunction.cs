namespace SqlArtisan.Internal;

public sealed class CurrentTimestampFunction : SqlExpression
{
    private readonly string? _precision;

    internal CurrentTimestampFunction(int? precision)
    {
        // The position takes an unsigned integer, so a negative one is valid nowhere
        // (ADR 0012); an engine's own ceiling is left to it (PostgreSQL clamps one).
        if (precision < 0)
        {
            throw new ArgumentException("CURRENT_TIMESTAMP precision must not be negative.");
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
