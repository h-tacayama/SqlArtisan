namespace InlineSqlSharp;

public sealed class ToCharFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal ToCharFunction(
        AbstractExpr expr,
        AbstractExpr? format = null)
    {
        _core = new(Keywords.TO_CHAR, expr, format);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
