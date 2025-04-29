using System.Numerics;

namespace SqlArtisan;

internal sealed class Literal(string value) : AbstractExpr
{
    private readonly string _value = value;

    public static Literal Of(char value) => new(value.ToString());

    public static Literal Of(string value) => new(value);

    public static Literal Of<TValue>(TValue value)
        where TValue : notnull, INumber<TValue> =>
        new(value?.ToString() ?? string.Empty);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_value);
}
