namespace InlineSqlSharp;

public sealed class RegexpReplaceFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal RegexpReplaceFunction(
        AbstractExpr source,
        AbstractExpr pattern,
        AbstractExpr replacement,
        AbstractExpr? position = null,
        AbstractExpr? occurrence = null,
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

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
