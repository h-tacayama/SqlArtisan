namespace InlineSqlSharp;

public sealed class RegexpCountFunction(
    AbstractExpr source,
    AbstractExpr pattern,
    AbstractExpr? position = null,
    RegexpOptions? options = null) : AbstractExpr
{
    private readonly VariadicFunctionCore _core = new(
        Keywords.REGEXP_COUNT,
        source,
        pattern,
        position,
        options?.ToValue());

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
