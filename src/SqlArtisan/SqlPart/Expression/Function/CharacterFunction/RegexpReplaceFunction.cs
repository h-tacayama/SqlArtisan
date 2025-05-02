namespace SqlArtisan;

public sealed class RegexpReplaceFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal RegexpReplaceFunction(
        SqlExpression source,
        SqlExpression pattern,
        SqlExpression replacement,
        SqlExpression? position = null,
        SqlExpression? occurrence = null,
        RegexpOptions? options = null)
    {
        _core = new(
            Keywords.RegexpReplace,
            source,
            pattern,
            replacement,
            position,
            occurrence,
            options?.ToValue());
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
