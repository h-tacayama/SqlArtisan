namespace InlineSqlSharp;

public sealed class TruncFunction(
    AbstractExpr expr,
    AbstractExpr? format = null) : AbstractExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.TRUNC, expr, format);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
