namespace SqlArtisan.Internal;

// The constant key of -> / ->>, emitted inline: MySQL's grammar takes only a
// literal there, and an expression index matches only a literal key (ADR 0016).
internal sealed class JsonKeyLiteral : SqlExpression
{
    private readonly string? _key;
    private readonly string? _index;

    internal JsonKeyLiteral(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _key = key;
    }

    internal JsonKeyLiteral(int index)
    {
        _index = index.ToInvariantString();
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (_key is { } key)
        {
            buffer.AppendStringLiteral(key);
        }
        else
        {
            buffer.Append(_index);
        }
    }
}
