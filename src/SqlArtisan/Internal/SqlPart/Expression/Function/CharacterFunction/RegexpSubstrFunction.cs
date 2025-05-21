namespace SqlArtisan.Internal;

public sealed class RegexpSubstrFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal RegexpSubstrFunction(
        SqlExpression source,
        SqlExpression pattern,
        SqlExpression? position = null,
        SqlExpression? occurrence = null,
        RegexpOptions? options = null,
        SqlExpression? subPatternPos = null)
    {
        _core = new(
            Keywords.RegexpSubstr,
            source,
            pattern,
            position,
            occurrence,
            options?.ToValue(),
            subPatternPos);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
