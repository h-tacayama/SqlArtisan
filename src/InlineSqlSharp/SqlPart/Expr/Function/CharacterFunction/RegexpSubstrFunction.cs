namespace InlineSqlSharp;

public sealed class RegexpSubstrFunction(
    AbstractExpr source,
    AbstractExpr pattern,
    AbstractExpr? position = null,
    AbstractExpr? occurrence = null,
    RegexpOptions? options = null,
    AbstractExpr? subPatternPos = null) : AbstractExpr
{
    private readonly VariadicFunctionCore _core = new(
        Keywords.REGEXP_SUBSTR,
        source,
        pattern,
        position,
        occurrence,
        options?.ToValue(),
        subPatternPos);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
