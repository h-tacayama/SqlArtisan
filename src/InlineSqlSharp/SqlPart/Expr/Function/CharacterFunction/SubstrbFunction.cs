namespace InlineSqlSharp;

public sealed class SubstrbFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal SubstrbFunction(
        AbstractExpr source,
        AbstractExpr position,
        AbstractExpr? length = null)
    {
        _core = new(Keywords.SUBSTRB, source, position, length);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
