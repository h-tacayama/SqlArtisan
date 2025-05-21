namespace SqlArtisan.Internal;

public sealed class RegexpCountFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal RegexpCountFunction(
        SqlExpression source,
        SqlExpression pattern,
        SqlExpression? position = null,
        RegexpOptions? options = null)
    {
        _core = new(
            Keywords.RegexpCount,
            source,
            pattern,
            position,
            options?.ToValue());
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
