using System.Numerics;

namespace SqlArtisan;

internal sealed class LiteralValue(string value) : SqlExpression
{
    private readonly string _value = value;

    public static LiteralValue Of(char value) => new(value.ToString());

    public static LiteralValue Of(string value) => new(value);

    public static LiteralValue Of<TValue>(TValue value)
        where TValue : notnull, INumber<TValue> =>
        new(value?.ToString() ?? string.Empty);

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(_value);
}
