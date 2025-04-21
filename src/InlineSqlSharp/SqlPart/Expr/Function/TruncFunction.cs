namespace InlineSqlSharp;

public sealed class TruncFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal TruncFunction(
        AbstractExpr expr,
        AbstractExpr? format = null)
    {
        _core = new(Keywords.TRUNC, expr, format);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
