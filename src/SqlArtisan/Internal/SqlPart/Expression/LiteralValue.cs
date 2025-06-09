using System.Numerics;

namespace SqlArtisan.Internal;

internal sealed class LiteralValue(string value) : SqlExpression
{
    private readonly string _value = value;

    internal static LiteralValue Of(char value) => new(value.ToString());

    internal static LiteralValue Of(string value) => new(value);

    internal static LiteralValue Of<TValue>(TValue value)
        where TValue : notnull, INumber<TValue> =>
        new(value?.ToString() ?? string.Empty);

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(_value);
}
