namespace InlineSqlSharp;

public sealed class ToNumberFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal ToNumberFunction(
        AbstractExpr expr,
        AbstractExpr? format = null)
    {
        _core = new(Keywords.TO_NUMBER, expr, format);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
