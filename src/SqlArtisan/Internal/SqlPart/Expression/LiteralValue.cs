namespace SqlArtisan.Internal;

internal sealed class LiteralValue(string value) : SqlExpression
{
    private readonly string _value = value;

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(_value);
}
