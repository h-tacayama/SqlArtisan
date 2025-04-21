namespace InlineSqlSharp;

public sealed class RegexpReplaceFunction(
    AbstractExpr source,
    AbstractExpr pattern,
    AbstractExpr replacement,
    AbstractExpr? position = null,
    AbstractExpr? occurrence = null,
    RegexpOptions? options = null) : AbstractExpr
{
    private readonly VariadicFunctionCore _core = new(
        Keywords.REGEXP_REPLACE,
        source,
        pattern,
        replacement,
        position,
        occurrence,
        options?.ToValue());

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
