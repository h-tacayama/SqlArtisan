namespace InlineSqlSharp;

public sealed class RegexpSubstrFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal RegexpSubstrFunction(
        AbstractExpr source,
        AbstractExpr pattern,
        AbstractExpr? position = null,
        AbstractExpr? occurrence = null,
        RegexpOptions? options = null,
        AbstractExpr? subPatternPos = null)
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

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
