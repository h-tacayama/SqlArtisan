namespace InlineSqlSharp;

public sealed class MinFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal MinFunction(AbstractExpr expr)
    {
        _core = new UnaryFunctionCore(Keywords.MIN, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
