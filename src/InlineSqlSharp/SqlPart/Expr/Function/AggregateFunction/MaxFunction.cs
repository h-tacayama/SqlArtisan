namespace InlineSqlSharp;

public sealed class MaxFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal MaxFunction(AbstractExpr expr)
    {
        _core = new UnaryFunctionCore(Keywords.MAX, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
