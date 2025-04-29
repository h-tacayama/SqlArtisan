namespace InlineSqlSharp;

public sealed class RegexpCountFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal RegexpCountFunction(
        AbstractExpr source,
        AbstractExpr pattern,
        AbstractExpr? position = null,
        RegexpOptions? options = null)
    {
        _core = new(
            Keywords.RegexpCount,
            source,
            pattern,
            position,
            options?.ToValue());
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
