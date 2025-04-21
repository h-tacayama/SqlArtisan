namespace InlineSqlSharp;

public sealed class ToCharFunction(
    AbstractExpr expr,
    AbstractExpr? format = null) : AbstractExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.TO_CHAR, expr, format);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
